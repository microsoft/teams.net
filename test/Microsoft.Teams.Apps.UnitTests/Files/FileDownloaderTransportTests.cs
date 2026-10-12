// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.Files;

/// <summary>
/// Transport invariants for the file download request. A download URL embeds its own
/// <c>tempauth</c> credential and points at a third-party storage host, so the request must not carry bot
/// credentials: attaching one can get the request rejected and sends the credential to a host with no business
/// seeing it.
/// <para><b>Where the invariant actually lives.</b> <see cref="FileDownloader"/> does not strip anything. It issues a
/// plain GET and inherits whatever its <see cref="HttpClient"/> carries, because <see cref="HttpClient"/> merges
/// <c>DefaultRequestHeaders</c> into every request. The guarantee therefore rests entirely on the registration:
/// <c>AddHttpClient&lt;FileDownloader&gt;()</c> adds no credential of its own, and the <c>AddBotHttpClient</c> call above it,
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
    private static ServiceProvider BuildAppContainer(HttpMessageHandler handler, Action<HttpClientFactoryOptions>? extraConfig = null)
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

    /// <summary>
    /// The accessor and the dispatcher share one scope check. This walks both, so a group chat file an agentic user's <see cref="FilesAccessor.ListAsync(CancellationToken)"/> surfaces is shown to also download, through Graph and as the agent.
    /// </summary>
    [Fact]
    public async Task Download_FetchesAGroupChatFileTheAccessorSurfacedForAnAgenticUser_ThroughGraphAsTheAgent()
    {
        // Unencoded, spaces included, the way the platform delivers it.
        const string contentUrl = "https://contoso.sharepoint.com/personal/a/Documents/Microsoft Teams Chat Files/report.txt";

        CoreActivity core = new() { Type = TeamsActivityTypes.Message };
        core.Properties["attachments"] = JsonSerializer.SerializeToElement<IList<TeamsAttachment>>(
        [
            new TeamsAttachment
            {
                ContentType = AttachmentContentType.FileDownloadInfo,
                ContentUrl = new Uri(contentUrl),
                Name = "report.txt",
                Content = new { uniqueId = "odsp-unique-id", fileType = "txt" },
            }
        ]);

        Conversation conversation = new("conv-1");
        conversation.Properties["conversationType"] = JsonSerializer.SerializeToElement("groupChat");
        core.Conversation = conversation;

        RecordingHandler handler = new();
        GraphCredential credential = new(FileActor.AgenticUser, _ => Task.FromResult<string?>("agent-token"));
        FilesAccessor accessor = new(MessageActivity.FromActivity(core), NullLogger.Instance, new FileDownloader(new HttpClient(handler)), credential);

        IncomingFile? file = await accessor.FirstAsync();

        Assert.NotNull(file);
        Assert.Equal("bytes", (await file.DownloadAsync()).Text());
        HttpRequestMessage request = handler.Last;
        Assert.Contains($"/shares/{GraphShare.EncodeSharingUrl(contentUrl)}/driveItem/content", request.RequestUri!.OriginalString, StringComparison.Ordinal);
        Assert.Contains("agent-token", request.Headers.Authorization?.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Answers with a response whose final hop left HTTPS, which is what a storage 302 to a plaintext host produces once the handler has followed it.</summary>
    private sealed class DowngradingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes("bytes")),
                // The redirect has already been followed by the time a handler returns, so the downgrade is visible
                // here rather than as a 302: this is the shape the real handler hands back.
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://storage.example/bytes"),
            });
    }

    [Fact]
    public async Task Download_RefusesAResponseWhoseFinalHopLeftHttps()
    {
        // Storage answers with a 302 to the host actually holding the bytes, so redirects have to be followed. A hop
        // down to plaintext would put the file on the wire in the clear, and the downloader is handed an HttpClient it
        // does not own, so it cannot turn redirects off. It refuses the response instead, before reading the body.
        DowngradingHandler handler = new();
        using ServiceProvider provider = BuildAppContainer(handler);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PersonalFile(provider.GetRequiredService<FileDownloader>()).DownloadAsync());

        Assert.Contains("must use https", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Download_AllowsAResponseThatStayedOnHttps()
    {
        // The ordinary storage 302. The guard cannot simply refuse every redirected response.
        RecordingHandler handler = new();
        using ServiceProvider provider = BuildAppContainer(handler);

        DownloadedFile downloaded = await PersonalFile(provider.GetRequiredService<FileDownloader>()).DownloadAsync();

        Assert.Equal("bytes", Encoding.UTF8.GetString(downloaded.Bytes.ToArray()));
    }

    /// <summary>A loopback HTTP server that answers each request with the next canned response and records the headers it arrived with.</summary>
    private sealed class LoopbackServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

        public List<string> RequestHeaders { get; } = [];

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public LoopbackServer() => _listener.Start();

        public async Task ServeAsync(params Func<string>[] responses)
        {
            foreach (Func<string> response in responses)
            {
                using TcpClient client = await _listener.AcceptTcpClientAsync();
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII, leaveOpen: true);

                StringBuilder headers = new();
                for (string? line = await reader.ReadLineAsync(); !string.IsNullOrEmpty(line); line = await reader.ReadLineAsync())
                {
                    headers.AppendLine(line);
                }

                RequestHeaders.Add(headers.ToString());
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response()));
            }
        }

        public static string Redirect(string location) => $"HTTP/1.1 302 Found\r\nLocation: {location}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";

        public static string Ok() => "HTTP/1.1 200 OK\r\nContent-Length: 5\r\nConnection: close\r\n\r\nbytes";

        public void Dispose() => _listener.Stop();
    }

    /// <summary>
    /// Storage answers with a 302 to the host actually holding the bytes, so redirects must be followed.
    /// The downloader's client keeps <c>Authorization</c> on a hop that stays on the original scheme, host, and port, and drops it on a hop to another host or port, so a token never reaches a host it was not sent to.
    /// Runs the shipped registration against loopback servers, because a fake primary handler would not exercise the redirect.
    /// </summary>
    [Theory]
    [InlineData("a different host", false)]
    [InlineData("another port on the original host", false)]
    [InlineData("the original host", true)]
    public async Task DiRegisteredClient_KeepsAuthorizationOnlyOnASameOriginRedirect(string destination, bool keepsAuthorization)
    {
        using LoopbackServer origin = new();
        using LoopbackServer otherPort = new();

        Task serving = destination switch
        {
            "a different host" => origin.ServeAsync(() => LoopbackServer.Redirect($"http://localhost:{origin.Port}/next"), LoopbackServer.Ok),
            "another port on the original host" => Task.WhenAll(
                origin.ServeAsync(() => LoopbackServer.Redirect($"http://127.0.0.1:{otherPort.Port}/next")),
                otherPort.ServeAsync(LoopbackServer.Ok)),
            _ => origin.ServeAsync(() => LoopbackServer.Redirect($"http://127.0.0.1:{origin.Port}/next"), LoopbackServer.Ok),
        };
        LoopbackServer destinationServer = destination == "another port on the original host" ? otherPort : origin;

        using ServiceProvider provider = BuildShippedContainer();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(FileDownloader));
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"http://127.0.0.1:{origin.Port}/start"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-credential");

        using HttpResponseMessage response = await client.SendAsync(request);
        await serving;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Authorization: Bearer test-credential", origin.RequestHeaders[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/next", destinationServer.RequestHeaders[^1], StringComparison.Ordinal);

        if (keepsAuthorization)
        {
            Assert.Contains("test-credential", destinationServer.RequestHeaders[^1], StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("Authorization", destinationServer.RequestHeaders[^1], StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiRegisteredClient_FollowsRedirectsThroughTheFileRedirectHandler()
    {
        using ServiceProvider provider = BuildShippedContainer();
        HttpMessageHandler? handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(nameof(FileDownloader));

        bool hasRedirectHandler = false;
        while (handler is DelegatingHandler delegating)
        {
            hasRedirectHandler |= delegating is FileRedirectHandler;
            handler = delegating.InnerHandler;
        }

        Assert.True(hasRedirectHandler);
        HttpClientHandler primary = Assert.IsType<HttpClientHandler>(handler);
        Assert.False(primary.AllowAutoRedirect);
    }

    /// <summary>The container the hosting extensions build, with every registration left as shipped, primary handler included.</summary>
    private static ServiceProvider BuildShippedContainer()
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

        return services.BuildServiceProvider();
    }
}
