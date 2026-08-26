// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for inbound file handling as shipped today (PR A), covering the two things unit tests
/// structurally cannot reach: that <see cref="FileDownloader"/> is actually wired into the DI container the app
/// builds, and that its byte-fetch contract holds against a real Microsoft endpoint over a real network.
/// <para><b>What these do and do not prove.</b> High fidelity for the <em>contract</em> the downloader depends on:
/// an https URL fetched with a plain unauthenticated GET, streamed with <c>ResponseHeadersRead</c>, returning bytes
/// or a clean 401. Low fidelity for <em>provenance</em>: these URLs are not Teams <c>tempauth</c> URLs, because no
/// automated route mints one. **These must never be described as end-to-end file receive.** Manual e2e remains the
/// only evidence for that.</para>
/// <para>Graph is used purely as a conveniently stable first-party HTTP endpoint. No token is sent and no Graph
/// application permission is required, so these run wherever the rest of the suite runs.</para>
/// </summary>
public class FilesIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public FilesIntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    // Public, unauthenticated, returns real bytes with a real content-type. Stands in for the "URL that serves the
    // file" half of the contract.
    private static readonly Uri PublicBytesUrl = new("https://graph.microsoft.com/v1.0/$metadata");

    // Requires auth, so an unauthenticated GET is a genuine 401 from a live Microsoft service. Stands in for an
    // expired `tempauth` URL.
    private static readonly Uri UnauthorizedUrl = new("https://graph.microsoft.com/v1.0/me");

    private FileDownloader Downloader => _f.ServiceProvider.GetRequiredService<FileDownloader>();

    // No Timeout: this one performs no I/O, and xUnit only supports Timeout on async tests.
    [Fact]
    [Trait("Category", "Files")]
    public void FileDownloader_ResolvesFromTheContainerTheAppBuilds()
    {
        // Catches removal of `AddHttpClient<Files.FileDownloader>()`. TeamsBotApplication falls back to
        // FileDownloader.CreateDefault() silently and with no log, so a broken registration degrades to a
        // process-wide static HttpClient with no signal and every unit test still passing.
        FileDownloader downloader = _f.ServiceProvider.GetRequiredService<FileDownloader>();

        Assert.NotNull(downloader);

        // Typed clients are transient, so two resolutions are distinct instances. Asserting that rather than
        // singleton-ness documents the registration's actual lifetime instead of guessing at it.
        Assert.NotSame(downloader, _f.ServiceProvider.GetRequiredService<FileDownloader>());
        _output.WriteLine("FileDownloader resolved from DI container");
    }

    [Fact(Timeout = 15000)]
    [Trait("Category", "Files")]
    public async Task OpenFileStream_FetchesRealBytesOverTheRealNetwork()
    {
        await using OpenedFileStream stream = await Downloader.OpenFileStreamAsync(
            ConversationType.Personal, PublicBytesUrl, contentType: null, priorFetchSucceeded: false, CancellationToken.None);

        // Reading only a prefix is deliberate: it proves HttpCompletionOption.ResponseHeadersRead really streams
        // rather than buffering the whole body, which a fake handler cannot demonstrate.
        byte[] head = new byte[64];
        int read = await stream.ReadAsync(head, CancellationToken.None);

        Assert.True(read > 0);
        Assert.Equal(PublicBytesUrl, stream.SourceUrl);

        // Resolved from the live response, not from the fallback. Real header casing and parameter handling.
        Assert.StartsWith("application/xml", stream.ContentType, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine($"Fetched {read} bytes, contentType={stream.ContentType}");
    }

    [Fact(Timeout = 15000)]
    [Trait("Category", "Files")]
    public async Task OpenFileStream_MapsARealUnauthorizedResponseOntoFileUrlExpired()
    {
        // The failure mode this exists for: if the platform ever answered an expired URL with a redirect to a login
        // page, or 200 plus an HTML login body instead of 401, every mocked test would still pass and the downloader
        // would hand callers HTML as if it were the file. Only a live call can tell the difference.
        FileUrlExpiredException ex = await Assert.ThrowsAsync<FileUrlExpiredException>(
            () => Downloader.OpenFileStreamAsync(
                ConversationType.Personal, UnauthorizedUrl, contentType: null, priorFetchSucceeded: false, CancellationToken.None));

        Assert.Equal(FileUrlExpiredReason.FirstFetch, ex.Reason);
        _output.WriteLine($"Live 401 mapped to FileUrlExpiredException: {ex.Reason}");
    }

    [Fact(Timeout = 15000)]
    [Trait("Category", "Files")]
    public async Task OpenFileStream_ReportsRereadWhenAPriorFetchSucceeded()
    {
        // Same live 401, opposite reason. Pins that the caller-supplied re-fetch state, not the response, chooses
        // between FirstFetch and Reread.
        FileUrlExpiredException ex = await Assert.ThrowsAsync<FileUrlExpiredException>(
            () => Downloader.OpenFileStreamAsync(
                ConversationType.Personal, UnauthorizedUrl, contentType: null, priorFetchSucceeded: true, CancellationToken.None));

        Assert.Equal(FileUrlExpiredReason.Reread, ex.Reason);
    }

    [Fact(Timeout = 15000)]
    [Trait("Category", "Files")]
    public async Task OpenFileStream_RejectsUnsupportedScopesAndNonHttpsUrls_BeforeAnyNetworkCall()
    {
        // Non-personal scopes have no receive path yet; this pins the shipped behaviour so PR J has to change it
        // deliberately rather than by accident.
        FileScopeNotSupportedException scopeEx = await Assert.ThrowsAsync<FileScopeNotSupportedException>(
            () => Downloader.OpenFileStreamAsync(
                ConversationType.GroupChat, PublicBytesUrl, contentType: null, priorFetchSucceeded: false, CancellationToken.None));
        Assert.Equal(ConversationType.GroupChat, scopeEx.Scope);

        InvalidOperationException httpEx = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Downloader.OpenFileStreamAsync(
                ConversationType.Personal, new Uri("http://graph.microsoft.com/v1.0/$metadata"), contentType: null, priorFetchSucceeded: false, CancellationToken.None));
        Assert.Contains("must use https", httpEx.Message, StringComparison.Ordinal);
    }
}
