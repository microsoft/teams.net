// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// Transport invariants for the file download request. A download URL embeds its own
/// <c>tempauth</c> credential and points at a third-party storage host, so the request must not carry bot
/// credentials: attaching one can get the request rejected and sends the credential to a host with no business
/// seeing it.
/// <para><b>Where the invariant actually lives.</b> <see cref="FileDownloader"/> does not strip anything. It issues a
/// plain GET and inherits whatever its <see cref="HttpClient"/> carries, because <see cref="HttpClient"/> merges
/// <c>DefaultRequestHeaders</c> into every request. The guarantee therefore rests entirely on the registration:
/// <c>AddHttpClient&lt;FileDownloader&gt;()</c> is deliberately bare, and the <c>AddBotHttpClient</c> call above it,
/// which is what attaches bot authentication, is deliberately not applied to it.</para>
/// <para>These tests assert the invariant at the level that holds it, the DI container, rather than at a level that would require changing the shipped downloader.</para>
/// <para>This matters now that the downloader has a Graph path that <em>does</em> authenticate. One path attaches a
/// credential and the other must not, so "nobody has configured one" is no longer the whole guarantee: the Graph
/// branch sets <c>Authorization</c> on its own request, never on the client, and the tests below assert both halves
/// against the shipped registration.</para>
/// </summary>
public class FileDownloaderTransportTests
{
    private const string DownloadUrl = "https://download.example/notes.txt?tempauth=abc";
    private const string ContentUrl = "https://contoso.sharepoint.com/personal/a/Documents/notes.txt";

    /// <summary>Records the outbound request and answers with a canned body, so nothing reaches the network.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public HttpRequestMessage Last => Assert.Single(Requests);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("bytes")),
            });
        }
    }

    private static IncomingFile PersonalFile(FileDownloader downloader)
        => new("notes.txt", ConversationType.Personal, FileSource.BotActivity, downloader)
        {
            DownloadUrl = new Uri(DownloadUrl),
        };

    /// <summary>A file shaped the way an Agentic User receives one: a browsable content URL and no download URL, so Graph is the only route to the bytes.</summary>
    private static IncomingFile AgenticFile(FileDownloader downloader)
        => new("notes.txt", ConversationType.Personal, FileSource.BotActivity, downloader)
        {
            ContentUrl = new Uri(ContentUrl),
            Credential = new GraphCredential(FileActor.AgenticUser, _ => Task.FromResult<string?>("agent-token")),
        };

    /// <summary>
    /// Builds the container the hosting extensions build, then overrides only the primary handler of the typed
    /// client they already registered. Deliberately does <b>not</b> call <c>AddHttpClient&lt;FileDownloader&gt;()</c>
    /// itself: doing so would register the downloader independently, so these tests would keep passing even if the
    /// hosting extensions stopped registering it at all. Reaching into the existing
    /// <see cref="HttpClientFactoryOptions"/> instead means resolution fails outright if the production registration
    /// goes away, which is the regression these tests exist to catch.
    /// <para>Every other piece of the shipped registration, including any handler or default header it configures,
    /// is left intact, so what the recorder sees is what a real download would send.</para>
    /// </summary>
    private static ServiceProvider BuildAppContainer(RecordingHandler handler, Action<HttpClientFactoryOptions>? extraConfig = null)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "files-transport-client-id",
                ["AzureAd:TenantId"] = "files-transport-tenant-id",
            })
            .Build());
        services.AddLogging();
        services.AddTeamsBotApplication();

        // The name AddHttpClient<FileDownloader>() registers its options under.
        services.Configure<HttpClientFactoryOptions>(nameof(FileDownloader), options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = handler);
            extraConfig?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DiRegisteredDownloader_SendsNoAuthorizationHeader()
    {
        RecordingHandler handler = new();
        using ServiceProvider provider = BuildAppContainer(handler);

        await PersonalFile(provider.GetRequiredService<FileDownloader>()).DownloadAsync();

        // Goes red if anyone attaches bot auth to this registration, adds a credential default header, or applies a
        // global auth handler across AddHttpClient registrations. That is the regression worth catching: the code
        // comment at the registration site asks for this, and nothing else enforces it.
        Assert.False(handler.Last.Headers.Contains("Authorization"));
        Assert.Null(handler.Last.Headers.Authorization);
    }

    [Fact]
    public async Task DiRegisteredDownloader_SendsAPlainGetToTheDownloadUrl()
    {
        RecordingHandler handler = new();
        using ServiceProvider provider = BuildAppContainer(handler);

        await PersonalFile(provider.GetRequiredService<FileDownloader>()).DownloadAsync();

        Assert.Equal(HttpMethod.Get, handler.Last.Method);
        Assert.Equal(new Uri(DownloadUrl), handler.Last.RequestUri);
        Assert.Null(handler.Last.Content);
    }

    /// <summary>
    /// Characterizes the shipped behaviour that makes the test above the one that matters: the downloader has no
    /// defence of its own. This is documentation of a constraint, not an endorsement of it.
    /// <para>Both per-request escapes were measured against a raw TCP listener on 2026-08-27 and neither works.
    /// <c>request.Headers.Authorization = null</c> leaves the client default to merge in, so the token still reaches
    /// the wire. <c>TryAddWithoutValidation("Authorization", null)</c> does suppress the token, but writes a bare
    /// <c>Authorization:</c> with no value, which is a malformed header sent to a host that never wanted one. So the
    /// guarantee stays where it can actually be held, at the registration.</para>
    /// </summary>
    [Fact]
    public async Task Download_ForwardsClientDefaults_SoTheTypedClientMustStayCredentialFree()
    {
        RecordingHandler handler = new();
        HttpClient credentialed = new(handler);
        credentialed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token");
        credentialed.DefaultRequestHeaders.UserAgent.ParseAdd("teams.net-test/1.0");

        await PersonalFile(new FileDownloader(credentialed)).DownloadAsync();

        // HttpClient merges DefaultRequestHeaders into every request and offers no per-request opt-out, so a
        // credential on the client reaches the storage host. Hence the guarantee has to be held at registration.
        Assert.True(handler.Last.Headers.Contains("Authorization"));
        Assert.Equal(["teams.net-test/1.0"], handler.Last.Headers.GetValues("User-Agent"));
    }

    /// <summary>
    /// The same registration that must send no credential to third-party storage must still authenticate a Graph call, so a fix for one direction that silently broke the other would show up here.
    /// </summary>
    [Fact]
    public async Task DiRegisteredDownloader_SendsAuthorizationOnTheGraphPath()
    {
        RecordingHandler handler = new();
        using ServiceProvider provider = BuildAppContainer(handler);

        await AgenticFile(provider.GetRequiredService<FileDownloader>()).DownloadAsync();

        Assert.Equal("Bearer agent-token", handler.Last.Headers.Authorization?.ToString());
    }

    /// <summary>
    /// The Graph branch must set the header per request rather than on the client, or the credential would ride along on the very next pre-authorized download through the same shared client.
    /// </summary>
    [Fact]
    public async Task GraphFetch_DoesNotLeaveACredentialOnTheSharedClient()
    {
        RecordingHandler handler = new();
        using ServiceProvider provider = BuildAppContainer(handler);
        FileDownloader downloader = provider.GetRequiredService<FileDownloader>();

        await AgenticFile(downloader).DownloadAsync();
        await PersonalFile(downloader).DownloadAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Bearer agent-token", handler.Requests[0].Headers.Authorization?.ToString());
        Assert.Null(handler.Requests[1].Headers.Authorization);
    }
}
