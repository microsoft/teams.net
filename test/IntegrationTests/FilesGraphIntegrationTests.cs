// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Live Microsoft Graph coverage for the <c>graphShare</c> retrieval path:
/// <c>GET {graphHost}/v1.0/shares/{u!token}/driveItem/content</c>.
/// <para><b>Why these are not unit tests.</b> Every assertion here is about what <em>Graph</em> does, and the unit
/// suite mocks the transport entirely, so it can only assert that our encoder matches our own expectation. Four
/// things live only on the wire: that Graph accepts a <c>u!</c> token minted from a <c>Uri</c>-normalized C# string;
/// that our client follows Graph's documented 302 to storage and ends up with bytes; which application permission
/// actually suffices, where Graph's own two reference pages disagree; and what a well-formed token for a missing
/// item returns, which decides whether one of the error branches is reachable at all.</para>
/// <para><b>Everything here runs as an Agentic User</b>, because the SDK does not perform file handling via app
/// identity or user-delegated permissions on the developer's behalf.</para>
/// <para><b>What they still do not prove.</b> Nothing here is end-to-end file receive. These URLs come from a file
/// the fixture seeded or a file an operator provisioned, not from a Teams attachment, because no automated route
/// mints one. Manual e2e through <c>samples/AIFileAnalysisBot</c> remains the only evidence for that.</para>
/// <para><b>Cost.</b> Zero Teams API calls, so nothing here touches the quota the runbook warns about. One token
/// acquisition and at most three Graph calls in fixture setup, one Graph call per test.</para>
/// </summary>
public class FilesGraphIntegrationTests : IClassFixture<GraphFilesFixture>
{
    private readonly GraphFilesFixture _f;
    private readonly ITestOutputHelper _output;

    public FilesGraphIntegrationTests(GraphFilesFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    private FileDownloader Downloader => _f.ServiceProvider.GetRequiredService<FileDownloader>();

    /// <summary>
    /// Skip, with the fixture's own account of what was missing, when a live Graph read could not be set up.
    /// Also records the permissions every result below was obtained under, so "it worked" is never ambiguous about
    /// which grant made it work.
    /// </summary>
    private void RequireResolvableItem()
    {
        Skip.If(_f.UnavailableReason is not null, _f.UnavailableReason ?? string.Empty);
        Skip.If(_f.ItemContentUrl is null, "no item was resolved");

        _output.WriteLine($"token permissions: {string.Join(", ", _f.TokenPermissions)}");
        _output.WriteLine($"item: {_f.ItemProvenance}");
    }

    private GraphCredential Credential(Action? onAcquire = null) => new(
        FileActor.AgenticUser,
        _ =>
        {
            onAcquire?.Invoke();
            return Task.FromResult(_f.AgentToken);
        },
        _f.GraphRoot);

    private static async Task<byte[]> DrainAsync(OpenedFileStream stream)
    {
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// The headline assertion: real bytes come back through the shipped downloader, for an item addressed only by
    /// its browsable URL.
    /// <para>Everything between the <c>contentUrl</c> and the bytes is real: our <c>u!</c> encoding of a
    /// <c>Uri</c>-normalized string, our <c>/v1.0/shares/{token}/driveItem/content</c> URL shape, a live Graph
    /// authorization decision, Graph's 302 to SharePoint, and our HTTP client following it. A mocked transport can
    /// reproduce the shape of this and none of the substance.</para>
    /// </summary>
    [SkippableFact(Timeout = 30000)]
    [Trait("Category", "Files")]
    public async Task GraphShare_FetchesRealBytes_ThroughTheShippedDownloader()
    {
        RequireResolvableItem();

        await using OpenedFileStream stream = await Downloader.OpenFileStreamAsync(
            ConversationType.Personal,
            downloadUrl: null,
            contentUrl: _f.ItemContentUrl,
            contentType: null,
            priorFetchSucceeded: false,
            credential: Credential(),
            CancellationToken.None);

        byte[] bytes = await DrainAsync(stream);

        Assert.NotEmpty(bytes);

        // The URL the downloader actually built, not one the test rebuilt. Asserting on it means a change to the
        // endpoint shape cannot pass by coincidence of some other URL also returning bytes.
        Assert.Contains("/v1.0/shares/u!", stream.SourceUrl.OriginalString, StringComparison.Ordinal);
        Assert.EndsWith("/driveItem/content", stream.SourceUrl.OriginalString, StringComparison.Ordinal);

        if (_f.SeededBytes.Length > 0)
        {
            // Only checkable when the fixture wrote the file. Proves the redirect landed on the right item rather
            // than on some other readable one, which a mere 200 would not.
            Assert.Equal(_f.SeededBytes, bytes);
        }

        _output.WriteLine($"fetched {bytes.Length} bytes, contentType={stream.ContentType}");
    }

    /// <summary>
    /// A well-formed sharing token for an item that does not exist collapses onto <c>AccessDenied</c>, not
    /// <c>NotFound</c>.
    /// <para><b>Measured against live Graph.</b> Four different
    /// "item is not there" shapes were tried against the BAMI test tenant: a missing file in a real folder, a
    /// missing folder on a real site, a nonexistent personal site, and an entirely foreign host. <b>All four returned
    /// 403 <c>accessDenied</c>, "The sharing link no longer exists, or you do not have permission to access it."</b>
    /// None returned 404. Only a malformed token behaves differently, at <c>400 invalidRequest</c>.</para>
    /// <para>So <c>FileRetrievalFailureReason.NotFound</c> has no producer on this path, and the <c>status is 404</c>
    /// branch is unreachable. That is Graph behaving correctly rather than a Graph bug: telling an unauthorized
    /// caller whether a resource exists is an information disclosure, so the two answers are deliberately merged.</para>
    /// <para>If Graph ever splits them, this goes red and <c>NotFound</c> becomes reachable, which is a
    /// change worth being told about.</para>
    /// </summary>
    [SkippableFact(Timeout = 30000)]
    [Trait("Category", "Files")]
    public async Task GraphShare_CollapsesAMissingItemOntoAccessDenied()
    {
        RequireResolvableItem();

        // Built by string append on OriginalString rather than through UriBuilder. UriBuilder writes the default port
        // back out explicitly (`https://host:443/...`), which changes the bytes the `u!` token encodes and therefore
        // the item it addresses, so the result would be attributable to the port rather than to the missing file.
        // That is the same Uri-normalization hazard this whole path carries in C#, met here in the test itself.
        string original = _f.ItemContentUrl!.OriginalString;
        int query = original.IndexOf('?', StringComparison.Ordinal);
        string suffix = $"-{Guid.NewGuid():N}-does-not-exist.txt";
        Uri missing = new(query < 0
            ? original + suffix
            : string.Concat(original.AsSpan(0, query), suffix, original.AsSpan(query)));

        FileRetrievalException ex = await Assert.ThrowsAsync<FileRetrievalException>(
            () => Downloader.OpenFileStreamAsync(
                ConversationType.Personal,
                downloadUrl: null,
                contentUrl: missing,
                contentType: null,
                priorFetchSucceeded: false,
                credential: Credential(),
                CancellationToken.None));

        _output.WriteLine($"missing item resolved to reason={ex.Reason}, details={ex.Details}");

        Assert.Equal(FileRetrievalFailureReason.AccessDenied, ex.Reason);
        Assert.Equal(FileActor.AgenticUser, ex.Actor);

        // ReadServiceErrorAsync returns null on an empty body, and an accessDenied carrying no message tells a
        // developer nothing about which of the two causes they are looking at.
        Assert.False(string.IsNullOrEmpty(ex.Details), "Graph returned no diagnosable error body for a missing item");
    }

    /// <summary>
    /// The Agentic User's Graph token carries a file-capable scope, as an assertion rather than a precondition.
    /// <para>An Agentic User owns a drive, so the fixture seeds into <c>/me/drive</c> and the tests above read
    /// the bytes back as the agent with no sharing step at all. Sharing is still the only way to reach a
    /// file the agent did not create, which is the real inbound shape, so manual e2e still carries that leg.</para>
    /// <para>What this adds over the fixture is the difference between a skip and a red. The fixture <em>skips</em>
    /// when the token carries no file scope, because an unconsented tenant should not produce a wall of red. But a
    /// blueprint whose Graph grant has lapsed then reports
    /// "skipped" and looks like nothing is wrong. This asserts the same condition so that case fails loudly.</para>
    /// </summary>
    // No Timeout: this reads state the fixture already acquired and makes no call of its own, and xUnit rejects
    // Timeout on a synchronous test.
    [SkippableFact]
    [Trait("Category", "Files")]
    public void AgenticUser_GraphToken_CarriesAFileCapableScope()
    {
        // A token that never arrived is an environment problem: no identity configured, an expired tenant, a network
        // failure. None of those are the condition under test, and failing on them would be a false red that teaches
        // people to ignore this. Only a token that DID arrive can be judged on what it carries.
        Skip.If(
            string.IsNullOrEmpty(_f.AgentToken),
            _f.UnavailableReason ?? "no Agentic User Graph token was acquired");

        // `.default` is what the SDK asks for, so what comes back is whatever the blueprint holds. Asserting that a
        // file-capable scope is among them is the only check that fails when a grant lapses; asking for the scope by
        // name instead returns AADSTS1002012 even when a strictly broader grant exists, which reads like a missing
        // permission and is not one.
        _output.WriteLine($"agentic token scopes: {string.Join(", ", _f.TokenPermissions)}");

        // Deliberately an assertion and not another Skip. The fixture skips this condition so an unconsented tenant
        // reports skips rather than red; that is right for the tests that need an item, and wrong here, because a
        // lapsed blueprint grant reporting "skipped" is exactly the failure this exists to catch.
        Assert.Contains(_f.TokenPermissions, s =>
            s.StartsWith("Files.", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("Sites.", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// An expired pre-authorized URL is terminal, against a real 401 from a real Microsoft endpoint.
    /// <para>The contentUrl is supplied deliberately. It is the locator a Graph fallback would use, so passing
    /// it proves the URL is terminal <em>because there is no recovery</em>, rather than because the test withheld
    /// the means to recover.</para>
    /// </summary>
    [SkippableFact(Timeout = 30000)]
    [Trait("Category", "Files")]
    public async Task ExpiredPreauthUrl_IsTerminal_AndDoesNotReachForGraph()
    {
        RequireResolvableItem();

        int acquisitions = 0;

        // A genuine live 401 from a first-party service, standing in for a lapsed `tempauth` URL. The existing
        // FilesIntegrationTests already leans on this endpoint for the same reason.
        Uri expired = new($"{_f.GraphRoot.ToString().TrimEnd('/')}/v1.0/me");

        await Assert.ThrowsAsync<FileUrlExpiredException>(
            () => Downloader.OpenFileStreamAsync(
                ConversationType.Personal,
                downloadUrl: expired,
                contentUrl: _f.ItemContentUrl,
                contentType: null,
                priorFetchSucceeded: true,
                credential: Credential(() => acquisitions++),
                CancellationToken.None));

        // No recovery was attempted, so no Graph token was ever needed. This is the assertion that would have caught
        // the removal silently regressing back into a fetch.
        Assert.Equal(0, acquisitions);

        _output.WriteLine("an expired pre-authorized URL failed terminally without reaching for a Graph token");
    }

}
