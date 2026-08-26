// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// Transport invariants for the file download request, as shipped today. A download URL embeds its own
/// <c>tempauth</c> credential and points at a third-party storage host, so the request must not carry bot
/// credentials: attaching one can get the request rejected and sends the credential to a host with no business
/// seeing it.
/// <para><b>Where the invariant actually lives.</b> <see cref="FileDownloader"/> does not strip anything. It issues a
/// plain GET and inherits whatever its <see cref="HttpClient"/> carries, because <see cref="HttpClient"/> merges
/// <c>DefaultRequestHeaders</c> into every request. The guarantee therefore rests entirely on the registration:
/// <c>AddHttpClient&lt;FileDownloader&gt;()</c> is deliberately bare, and the <c>AddBotHttpClient</c> call above it,
/// which is what attaches bot authentication, is deliberately not applied to it.</para>
/// <para>So these tests assert the invariant at the level that holds it, the DI container, rather than at a level
/// that would require changing the shipped downloader. teams.ts asserts the equivalent in
/// <c>download.http-client.spec.ts</c>; C# had no equivalent at any level.</para>
/// <para>This matters more once PR J lands, since it adds a Graph path on the same downloader that <em>does</em>
/// authenticate. Once one path attaches a credential and the other must not, "nobody has configured one" stops being
/// a safe place to leave the guarantee.</para>
/// </summary>
public class FileDownloaderTransportTests
{
    private const string DownloadUrl = "https://download.example/notes.txt?tempauth=abc";

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

    /// <summary>
    /// Builds the container the hosting extensions build, then swaps only the downloader's primary handler for a
    /// recorder. Every other piece of the registration, including any handler or default header it configures, is
    /// left exactly as shipped, so what the recorder sees is what a real download would send.
    /// </summary>
    private static ServiceProvider BuildAppContainer(RecordingHandler handler, Action<IHttpClientBuilder>? extraConfig = null)
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

        IHttpClientBuilder builder = services.AddHttpClient<FileDownloader>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        extraConfig?.Invoke(builder);

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
    /// defence of its own. This is documentation of a constraint, not an endorsement of it. If C# ever grows an
    /// explicit strip, as teams.ts has, this test is the one that should change.
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
}
