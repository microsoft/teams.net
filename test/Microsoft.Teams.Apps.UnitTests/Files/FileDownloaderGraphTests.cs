// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// The Graph <c>/shares</c> fetch route, and what happens when a pre-authorized URL expires.
/// </summary>
public class FileDownloaderGraphTests
{
    private static readonly Uri ContentUrl = new("https://contoso.sharepoint.com/personal/a/Documents/report.pdf");
    private static readonly Uri DownloadUrl = new("https://contoso.sharepoint.com/personal/a/_layouts/15/download.aspx?UniqueId=1");

    /// <summary>Records every request the dispatcher makes, so the number of requests a route makes is observable rather than inferred.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly IReadOnlyList<(HttpStatusCode Status, string? Body)> _responses;
        private int _index;

        public RecordingHandler(params (HttpStatusCode Status, string? Body)[] responses) => _responses = responses;

        public List<(Uri? Url, string? Authorization)> Calls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request.RequestUri, request.Headers.Authorization?.ToString()));

            (HttpStatusCode status, string? body) = _responses[Math.Min(_index++, _responses.Count - 1)];
            HttpResponseMessage response = new(status);

            if (status == HttpStatusCode.OK)
            {
                response.Content = new StringContent(body ?? "bytes", Encoding.UTF8, "application/pdf");
            }
            else if (body is not null)
            {
                response.Content = new StringContent(body);
            }

            return Task.FromResult(response);
        }
    }

    private static FileDownloader DownloaderFor(RecordingHandler handler) => new(new HttpClient(handler));

    private static GraphCredential Credential(FileActor actor, string? token, Uri? baseUrlRoot = null)
        => new(actor, _ => Task.FromResult(token), baseUrlRoot);

    private static readonly Func<RecordingHandler, GraphCredential> App = _ => Credential(FileActor.App, "app-token");

    // Passes the CouldReachDriveItems allowlist on its Sites. prefix, yet grants nothing until an admin grants specific sites.
    // The token names the permission, not the sites, so the gate cannot tell the two apart.
    private static readonly Func<RecordingHandler, GraphCredential> SitesSelected =
        _ => Credential(FileActor.App, Jwt(new { roles = new[] { "Sites.Selected" } }));
    private static readonly Func<RecordingHandler, GraphCredential> Agentic = _ => Credential(FileActor.AgenticUser, "agent-token");

    /// <summary>A JWT whose payload carries the given claims. Only the payload segment is ever read.</summary>
    private static string Jwt(object claims)
        => $"{Segment("{\"alg\":\"RS256\"}")}.{Segment(JsonSerializer.Serialize(claims))}.sig";

    private static string Segment(string json)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('/', '_').Replace('+', '-');

    private static Task<OpenedFileStream> OpenAsync(
        FileDownloader downloader,
        Uri? downloadUrl,
        Uri? contentUrl,
        GraphCredential? credential,
        bool priorFetchSucceeded = false)
        => downloader.OpenFileStreamAsync(
            ConversationType.Personal,
            downloadUrl,
            contentUrl,
            contentType: null,
            priorFetchSucceeded,
            credential,
            CancellationToken.None);

    // ==================== the Graph fetch route ====================

    [Fact]
    public async Task ResolvesBytesThroughShares_WhenNoDownloadUrlIsPresent()
    {
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler));

        (Uri? url, _) = Assert.Single(handler.Calls);
        Assert.Contains($"/shares/{GraphShare.EncodeSharingUrl(ContentUrl.OriginalString)}/driveItem/content", url!.OriginalString, StringComparison.Ordinal);
        Assert.Equal("application/pdf; charset=utf-8", opened.ContentType);
    }

    [Fact]
    public async Task AddressesTheSovereignHost_WithTheApiVersion_WhenTheCredentialCarriesOne()
    {
        // The configured value is a host root and the version is appended here rather than by a Graph client, so a mismatch produces a 404 that reads like a missing item.
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        GraphCredential sovereign = Credential(FileActor.App, "app-token", new Uri("https://graph.microsoft.us"));

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, sovereign);

        Assert.StartsWith("https://graph.microsoft.us/v1.0/shares/", handler.Calls[0].Url!.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NeverSendsAuthorization_OnThePreAuthorizedPath_EvenWhenACredentialIsAvailable()
    {
        // This URL carries its own credential and points at third-party storage, so a bot token must not ride along.
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), DownloadUrl, ContentUrl, App(handler));

        Assert.Equal(DownloadUrl, handler.Calls[0].Url);
        Assert.Null(handler.Calls[0].Authorization);
    }

    [Fact]
    public async Task UsesTheAgenticCredential_WhenOneIsSupplied()
    {
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler));

        Assert.Equal("Bearer agent-token", handler.Calls[0].Authorization);
    }

    [Fact]
    public async Task ReportsNoGraphCredential_BeforeMakingAnyRequest_WhenNoCredentialExists()
    {
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        await Assert.ThrowsAsync<FileRetrievalException>(() => OpenAsync(DownloaderFor(handler), null, ContentUrl, null));

        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task ReportsNoGraphCredential_BeforeMakingAnyRequest_WhenTheTokenResolvesEmpty()
    {
        // The app has no consented Graph application permissions. Detectable without a round trip, so this surfaces as a named failure rather than an opaque Graph 401.
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.App, null)));

        Assert.Equal(FileRetrievalFailureReason.NoGraphCredential, error.Reason);
        Assert.Equal(FileActor.App, error.Actor);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task CarriesTheAcquisitionFailure_AsDetails_WhenTheCredentialThrows()
    {
        // An acquisition that threw and an identity with no permissions both arrive here as "no token", but the fixes differ: one is a transient or configuration fault, the other is a consent problem. The canned guidance names consent, so without the cause a transient Entra failure reads as a permissions problem that is not there.
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        GraphCredential throwing = new(
            FileActor.AgenticUser,
            _ => throw new InvalidOperationException("AADSTS7000215: Invalid client secret provided."),
            null);

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, throwing));

        Assert.Equal(FileRetrievalFailureReason.NoGraphCredential, error.Reason);
        Assert.Equal(FileActor.AgenticUser, error.Actor);
        Assert.Equal("AADSTS7000215: Invalid client secret provided.", error.Details);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task ReportsNoGraphCredential_ForATokenCarryingNoRolesAndNoScopes_BeforeAnyRequest()
    {
        // Verified against real Graph 2026-08-26: an app-only token with an empty `roles` claim returns 401 generalException/spException, which is indistinguishable on the wire from a genuine denial but has a completely different fix.
        // Calling it accessDenied sends the developer to check file sharing when the real problem is that the app registration has no Graph permissions at all.
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        string roleless = Jwt(new { aud = "https://graph.microsoft.com", roles = Array.Empty<string>() });

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.App, roleless)));

        Assert.Equal(FileRetrievalFailureReason.NoGraphCredential, error.Reason);
        Assert.Equal(FileActor.App, error.Actor);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task ReportsNoGraphCredential_WhenEveryScopeIsNonFile_BeforeAnyRequest()
    {
        // The case `.default` creates and an emptiness check misses.
        // A blueprint consented to unrelated Graph permissions returns a POPULATED scp with nothing file-capable in it, so "carries any permission at all" passes and the developer gets a late 403 indistinguishable from "not shared with you".
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        string unrelated = Jwt(new { scp = "profile openid email Mail.Send Chat.ReadWrite User.Read.All" });

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.AgenticUser, unrelated)));

        Assert.Equal(FileRetrievalFailureReason.NoGraphCredential, error.Reason);
        Assert.Equal(FileActor.AgenticUser, error.Actor);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task Proceeds_WhenADelegatedTokenCarriesAFileCapableScope()
    {
        // The shape the live blueprint actually issues, measured 2026-09-09: `.default` returned eleven scopes, of which only Files.ReadWrite.All and Sites.Read.All are file-capable.
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        string capable = Jwt(new { scp = "profile openid email Mail.Send Files.ReadWrite.All Sites.Read.All" });

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.AgenticUser, capable));

        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task Proceeds_WhenTheTokenCarriesApplicationRoles()
    {
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        string token = Jwt(new { roles = new[] { "Files.Read.All" } });

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.App, token));

        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task Proceeds_WhenTheTokenCarriesDelegatedScopes_AsAnAgenticUserTokenDoes()
    {
        RecordingHandler handler = new((HttpStatusCode.OK, null));
        string token = Jwt(new { scp = "profile openid Files.Read.All" });

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.AgenticUser, token));

        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task FailsOpen_OnATokenThatIsNotADecodableJwt()
    {
        // An unexpected token shape must not block a fetch that might have worked.
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        await using OpenedFileStream opened = await OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.App, "not-a-jwt"));

        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task Maps403_ToAccessDenied_NamingTheActor()
    {
        RecordingHandler handler = new((HttpStatusCode.Forbidden, null));

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler)));

        Assert.Equal(FileRetrievalFailureReason.AccessDenied, error.Reason);
        Assert.Equal(FileActor.AgenticUser, error.Actor);
        Assert.Contains("the agentic user", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NamesTheIdentity_AndCarriesTheServiceMessage_OnAStatusItDoesNotMap()
    {
        // A status outside 401/403 is not a typed reason, so the only diagnosis a caller gets is what the service
        // said and who was refused. An identity with no provisioned drive is the case that makes this matter,
        // because Graph answers the drive lookup rather than the sharing token and the message is the only tell.
        RecordingHandler handler = new((HttpStatusCode.NotFound, """{"error":{"code":"ResourceNotFound","message":"Unable to retrieve user's mysite URL."}}"""));

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler)));

        Assert.Contains("AgenticUser", error.Message, StringComparison.Ordinal);
        Assert.Contains("404", error.Message, StringComparison.Ordinal);
        Assert.Contains("mysite", error.Message, StringComparison.Ordinal);
    }

    // ==================== an expired pre-authorized URL ====================

    // No pre-authorized URL means no prior error to fall back to, so AccessDenied is the right answer here.
    [Fact]
    public async Task IsTerminal_ThrowsFileUrlExpired()
    {
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, null));

        await Assert.ThrowsAsync<FileUrlExpiredException>(() => OpenAsync(DownloaderFor(handler), DownloadUrl, null, App(handler)));
    }

    [Fact]
    public async Task StillThrowsFileUrlExpired_WhenNoCredentialExists()
    {
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, null));

        await Assert.ThrowsAsync<FileUrlExpiredException>(() => OpenAsync(DownloaderFor(handler), DownloadUrl, ContentUrl, null));
    }

    [Fact]
    public async Task ThrowsFileUrlExpired_NotAGraphError_WhenTheAppNeverAdoptedGraph()
    {
        // The realistic shape of an existing bot: it has credentials, and real payloads always carry a contentUrl,
        // What it does not have is a consented Graph permission.
        // Reporting access-denied here would name a consent this app never asked for and would silently stop
        // matching any existing `catch (FileUrlExpiredException)`.
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, null));

        await Assert.ThrowsAsync<FileUrlExpiredException>(
            () => OpenAsync(DownloaderFor(handler), DownloadUrl, ContentUrl, Credential(FileActor.App, null)));

        // And it must not have spent a request finding that out.
        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task ThrowsFileUrlExpired_WhenAcquiringTheTokenFailsOutright()
    {
        // An Entra or network failure during acquisition must not overwrite the more specific error the caller
        // already holds.
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, null));
        GraphCredential broken = new(FileActor.App, _ => throw new InvalidOperationException("AADSTS50034: tenant unreachable"));

        await Assert.ThrowsAsync<FileUrlExpiredException>(() => OpenAsync(DownloaderFor(handler), DownloadUrl, ContentUrl, broken));
    }

    [Fact]
    public async Task StillReportsNoGraphCredential_OnAFileThatNeverHadAUrl()
    {
        // With no pre-authorized URL there is no expiry to report, so Graph's own failure is the only true account
        // of what went wrong.
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.AgenticUser, null)));

        Assert.Equal(FileRetrievalFailureReason.NoGraphCredential, error.Reason);
        Assert.Equal(FileActor.AgenticUser, error.Actor);
    }

    [Fact]
    public async Task LeavesAnAppWithUnrelatedGraphConsent_OnItsExistingExpiryError()
    {
        // The regression this guards: every app now carries an app credential, so merely resolving a token is not
        // evidence the app opted into file access. A bot that consented to, say, User.Read.All for its own reasons
        // must not be pulled onto the Graph path and handed a different error type than it has always seen.
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, null));
        GraphCredential unrelated = Credential(FileActor.App, Jwt(new { roles = new[] { "User.Read.All" } }));

        await Assert.ThrowsAsync<FileUrlExpiredException>(() => OpenAsync(DownloaderFor(handler), DownloadUrl, ContentUrl, unrelated));

        // One call, not two: nothing is attempted after the URL lapses.
        Assert.Single(handler.Calls);
    }

    [Fact]
    public async Task KeepsWhatTheServiceActuallySaid_OnADenial()
    {
        // `Reason` collapses an unconsented scope and a never-shared file into one AccessDenied, because the SDK
        // cannot tell them apart. The service can, and says so in prose, so dropping that text would destroy the
        // only signal that distinguishes them.
        RecordingHandler handler = new((HttpStatusCode.Forbidden, """{"error":{"code":"accessDenied","message":"The caller does not have permission"}}"""));

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler)));

        Assert.Equal(FileRetrievalFailureReason.AccessDenied, error.Reason);
        Assert.Equal("accessDenied: The caller does not have permission", error.Details);
    }

    [Fact]
    public async Task FallsBackToRawText_WhenTheServiceDoesNotReplyWithAGraphEnvelope()
    {
        // A 401 can come from the edge as HTML rather than Graph JSON, so the parser must not assume an envelope.
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, "<html><body>Access Denied</body></html>"));

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler)));

        Assert.Equal("<html><body>Access Denied</body></html>", error.Details);
    }

    [Fact]
    public async Task TruncatesAnOversizedServiceErrorBody()
    {
        // These are streaming responses, so an unbounded read on an error path is a hazard. The bound is what keeps
        // it safe; without it a large body would land whole in an exception message.
        RecordingHandler handler = new((HttpStatusCode.Forbidden, new string('x', 8192)));

        FileRetrievalException error = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Agentic(handler)));

        Assert.Equal(2048, error.Details!.Length);
    }

    [Fact]
    public async Task PointsEachActorAtTheRemedyThatActuallyAppliesToIt()
    {
        // An agent identity gets Graph scopes from its blueprint, so that arm links the agent permission model. The
        // app arm deliberately has no doc link: no permission grant would change the outcome, so pointing at a
        // permissions doc would advise a fix that does not work.
        RecordingHandler handler = new((HttpStatusCode.OK, null));

        FileRetrievalException agentic = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.AgenticUser, null)));
        FileRetrievalException app = await Assert.ThrowsAsync<FileRetrievalException>(
            () => OpenAsync(DownloaderFor(handler), null, ContentUrl, Credential(FileActor.App, null)));

        Assert.Contains("https://learn.microsoft.com/entra/agent-id/concept-inheritable-permissions", agentic.Message, StringComparison.Ordinal);
        Assert.Contains("not supported via the SDK", app.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsTheRereadReason_WhenAPriorFetchSucceeded()
    {
        RecordingHandler handler = new((HttpStatusCode.Unauthorized, null));

        FileUrlExpiredException error = await Assert.ThrowsAsync<FileUrlExpiredException>(
            () => OpenAsync(DownloaderFor(handler), DownloadUrl, null, App(handler), priorFetchSucceeded: true));

        Assert.Equal(FileUrlExpiredReason.Reread, error.Reason);
    }
}
