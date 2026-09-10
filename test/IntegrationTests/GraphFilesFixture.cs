// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Fixture for the Graph <c>/shares</c> file tests. Deliberately separate from <see cref="IntegrationTestFixture"/>
/// rather than reusing it.
/// <para><b>Why its own fixture.</b> These tests need exactly two things from DI, <see cref="FileDownloader"/> and
/// <see cref="BotTokenProvider"/>, and nothing from the Teams conversation. <see cref="IntegrationTestFixture"/>
/// demands seven <c>TEST_*</c> variables in its constructor and spends a <c>GetMembersAsync</c> call in
/// <c>InitializeAsync</c>, against the one quota the suite actually strains. Reusing it would have made a
/// Graph-only test cost a Teams API call and require a fully provisioned conversation to run. This fixture spends
/// <b>zero</b> Teams API calls: one token acquisition and, at most, three Graph calls.</para>
/// <para><b>What it prepares.</b> An <b>Agentic User's</b> Graph token, the scopes that token actually carries, and a
/// real drive item in the agent's own drive, addressable by a browsable <c>webUrl</c> (the same shape an inbound
/// attachment's <c>contentUrl</c> has). Preparation never throws: it records why it could not prepare, and each test
/// turns that into a <c>Skip</c>. An unconsented tenant therefore reports skips rather than a wall of red.</para>
/// <para><b>Why the agent and not the app.</b> The SDK does not perform file handling via app identity or
/// user-delegated permissions on the developer's behalf; an Agentic User is the one identity that reads a file as
/// itself. These tests originally ran on an app-only token; re-homing them keeps every assertion and moves it onto
/// the identity the SDK actually uses.</para>
/// <para><b>The agent can seed its own file.</b> Measured: an Agentic User owns a drive
/// (<c>GET /me/drive</c> returns 200 with <c>@odata.type: agentUser</c>), so the fixture writes into
/// <c>/me/drive</c> rather than the tenant root site, and reads it back as the same identity. That removes the
/// sharing step that made reading <em>as</em> an agent look impractical.</para>
/// </summary>
public sealed class GraphFilesFixture : IAsyncLifetime, IDisposable, ITestOutputHelperAccessor
{
    /// <summary>Public-cloud Graph host root. Sovereign runs override it with <c>TEST_GRAPH_BASEURL</c>.</summary>
    public Uri GraphRoot { get; }

    /// <summary>Scope requested for the agentic token. <c>.default</c> matches what all three SDKs ask for.</summary>
    public string GraphScope => $"{GraphRoot.ToString().TrimEnd('/')}/.default";

    public ServiceProvider ServiceProvider { get; }

    /// <summary>The Agentic User's Graph token, or <c>null</c> when acquisition failed.</summary>
    public string? AgentToken { get; private set; }

    /// <summary>
    /// Permissions the acquired token actually carries, decoded here rather than read back from the SDK.
    /// <para>Decoding it independently is the point: <c>FileDownloader</c> branches on this claim, so a test that
    /// asked the SDK what the token held would hide a bug in the SDK's own parser. The lines below are transcribed
    /// from the JWT spec, not from the SDK. Note an agentic token is <b>delegated-shaped</b>: its permissions arrive
    /// in <c>scp</c> rather than <c>roles</c>, which is why the decoder reads both.</para>
    /// </summary>
    public IReadOnlyList<string> TokenPermissions { get; private set; } = [];

    /// <summary>Browsable URL of the item under test, standing in for an attachment's <c>contentUrl</c>.</summary>
    public Uri? ItemContentUrl { get; private set; }

    /// <summary>Bytes written when the fixture seeded the item, empty when it was pre-provisioned.</summary>
    public byte[] SeededBytes { get; private set; } = [];

    /// <summary>How the item was obtained, for the test output to record alongside any result.</summary>
    public string ItemProvenance { get; private set; } = "none";

    /// <summary>Why preparation could not complete, or <c>null</c> when it did.</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>True when the token carries a permission that also allows writing, which disqualifies the read-only proof.</summary>
    public bool TokenCanWrite => TokenPermissions.Any(p =>
        p.StartsWith("Files.ReadWrite", StringComparison.OrdinalIgnoreCase)
        || p.StartsWith("Sites.ReadWrite", StringComparison.OrdinalIgnoreCase)
        || p.Equals("Sites.FullControl.All", StringComparison.OrdinalIgnoreCase)
        || p.Equals("Sites.Manage.All", StringComparison.OrdinalIgnoreCase));

    /// <summary>True when the token carries a permission Graph documents as sufficient to read drive item content.</summary>
    public bool TokenCanReadFiles => TokenPermissions.Any(p =>
        p.StartsWith("Files.", StringComparison.OrdinalIgnoreCase)
        || p.StartsWith("Sites.", StringComparison.OrdinalIgnoreCase));

    public ITestOutputHelper? OutputHelper { get; set; }

    private readonly IHttpClientFactory _httpClientFactory;
    private string? _seededDriveId;
    private string? _seededItemId;

    public GraphFilesFixture()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables()
            .Build();

        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            builder.AddXUnit(this);
            builder.AddFilter("System.Net", LogLevel.Warning);
            builder.AddFilter("Microsoft.Identity", LogLevel.Error);
            builder.AddFilter("Microsoft.Teams", LogLevel.Information);
        });
        services.AddSingleton(configuration);
        services.AddTeamsBotApplication();

        ServiceProvider = services.BuildServiceProvider();
        _httpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();

        GraphRoot = new Uri(Environment.GetEnvironmentVariable("TEST_GRAPH_BASEURL") ?? "https://graph.microsoft.com");
    }

    public async Task InitializeAsync()
    {
        string? agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
        string? agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");
        string? blueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId");

        if (string.IsNullOrEmpty(agenticAppId) || string.IsNullOrEmpty(agenticUserId) || string.IsNullOrEmpty(blueprintId))
        {
            UnavailableReason =
                "no agentic identity is configured. Set TEST_AGENTIC_APPID, TEST_AGENTIC_USERID and AzureAd__ClientId, "
                + "which is what the agenticid-*.runsettings files supply. These tests deliberately do not fall back to "
                + "an app-only token: the SDK does not read file bytes with an app identity, so a run on one would "
                + "not prove anything about the shipped path.";
            return;
        }

        AgenticIdentity identity = new()
        {
            AgenticUserId = agenticUserId,
            AgenticAppId = agenticAppId,
            AgenticAppBlueprintId = blueprintId
        };

        try
        {
            AgentToken = await ServiceProvider
                .GetRequiredService<BotTokenProvider>()
                .GetAgenticUserTokenAsync(identity, GraphScope, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            UnavailableReason = $"could not acquire an Agentic User Graph token: {ex.Message}";
            return;
        }

        if (string.IsNullOrEmpty(AgentToken))
        {
            UnavailableReason = "the Agentic User Graph token came back empty";
            return;
        }

        TokenPermissions = PermissionsOf(AgentToken);

        if (!TokenCanReadFiles)
        {
            // A token issues and carries nothing that can read a file, which has already happened once in this
            // workstream when a blueprint's Graph grant lapsed. Naming the scopes means a failed run says which is
            // missing rather than just that something went wrong.
            UnavailableReason = TokenPermissions.Count == 0
                ? "the Agentic User Graph token carries no scopes at all, so no Graph permission is consented on the blueprint"
                : $"the Agentic User Graph token carries no Files.* or Sites.* scope (has: {string.Join(", ", TokenPermissions)})";
            return;
        }

        await ResolveItemAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Obtain an item to read. A pre-provisioned one is preferred, because seeding needs a write permission the code
    /// under test does not, and a token holding that write permission cannot prove the read-only one suffices.
    /// </summary>
    private async Task ResolveItemAsync()
    {
        string? preProvisioned = Environment.GetEnvironmentVariable("TEST_FILE_CONTENT_URL");

        if (!string.IsNullOrEmpty(preProvisioned))
        {
            if (!Uri.TryCreate(preProvisioned, UriKind.Absolute, out Uri? parsed))
            {
                UnavailableReason = $"TEST_FILE_CONTENT_URL is not an absolute URL: '{preProvisioned}'";
                return;
            }

            ItemContentUrl = parsed;
            ItemProvenance = "pre-provisioned (TEST_FILE_CONTENT_URL)";
            await WaitUntilResolvableAsync().ConfigureAwait(false);
            return;
        }

        if (!TokenCanWrite)
        {
            UnavailableReason =
                "no TEST_FILE_CONTENT_URL is set and the agentic token carries no write scope, so the fixture cannot "
                + "seed an item into the agent's own drive. Either consent Files.ReadWrite.All on the blueprint, or "
                + "set TEST_FILE_CONTENT_URL to the webUrl of a file already shared with this agent.";
            return;
        }

        await SeedItemAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Create a small item the test owns outright, then delete it in <see cref="DisposeAsync"/>.
    /// <para>The name carries a space and a non-ASCII character on purpose. Those are the characters that make a
    /// SharePoint <c>webUrl</c> percent-encoded, which is where a <c>Uri</c>-normalized C# string can diverge from
    /// the bytes the platform sent. That divergence is invisible to a unit test, because a unit test asserts our
    /// encoder against our own expectation rather than against Graph's.</para>
    /// </summary>
    private async Task SeedItemAsync()
    {
        using HttpClient http = _httpClientFactory.CreateClient();

        try
        {
            _seededDriveId = Environment.GetEnvironmentVariable("TEST_GRAPH_DRIVE_ID");

            if (string.IsNullOrEmpty(_seededDriveId))
            {
                // The AGENT'S OWN drive, not the tenant root site. An Agentic User owns one (measured: this returns
                // 200 with @odata.type agentUser), so the agent can both write and read the item as itself. Seeding
                // anywhere else would need the file shared with the agent, which is the step that made reading as an
                // agent look impractical.
                using HttpResponseMessage driveResponse = await SendAsync(http, HttpMethod.Get, $"{Root}/v1.0/me/drive").ConfigureAwait(false);

                if (!driveResponse.IsSuccessStatusCode)
                {
                    UnavailableReason = $"could not resolve the Agentic User's own drive to seed into: {(int)driveResponse.StatusCode} {await BodyAsync(driveResponse).ConfigureAwait(false)}";
                    return;
                }

                using JsonDocument drive = JsonDocument.Parse(await driveResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
                _seededDriveId = drive.RootElement.GetProperty("id").GetString();
            }

            SeededBytes = Encoding.UTF8.GetBytes($"teams-sdk integration seed {Guid.NewGuid():N}\n");
            string name = $"teams-sdk integration {Guid.NewGuid():N} caf\u00e9.txt";
            string path = $"{Root}/v1.0/drives/{_seededDriveId}/root:/{Uri.EscapeDataString(name)}:/content";

            using HttpRequestMessage request = new(HttpMethod.Put, path)
            {
                Content = new ByteArrayContent(SeededBytes)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AgentToken);

            using HttpResponseMessage response = await http.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                UnavailableReason = $"could not seed a drive item: {(int)response.StatusCode} {await BodyAsync(response).ConfigureAwait(false)}";
                return;
            }

            using JsonDocument item = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            _seededItemId = item.RootElement.GetProperty("id").GetString();
            ItemContentUrl = new Uri(item.RootElement.GetProperty("webUrl").GetString()!);
            ItemProvenance = $"seeded by the fixture into drive {_seededDriveId}";

            await WaitUntilResolvableAsync().ConfigureAwait(false);
        }
#pragma warning disable CA1031 // preparation never throws; it records why it could not prepare
        catch (Exception ex)
#pragma warning restore CA1031
        {
            UnavailableReason = $"seeding threw: {ex.Message}";
        }
    }

    /// <summary>
    /// Poll <c>/shares/{token}/driveItem</c> until the item resolves, so a test failure means the byte fetch failed
    /// rather than that SharePoint had not caught up.
    /// <para>Whether <c>/shares</c> is immediately consistent after a <c>PUT</c> is <b>not verified</b>: it resolves a
    /// URL rather than querying an index, so it ought to be, but that was never measured. The poll costs nothing when
    /// the assumption holds and removes a flake class if it does not. It is metadata, not content, so it does not
    /// pre-empt what the tests assert.</para>
    /// </summary>
    private async Task WaitUntilResolvableAsync()
    {
        using HttpClient http = _httpClientFactory.CreateClient();
        string url = $"{Root}/v1.0/shares/{EncodeSharingUrl(ItemContentUrl!.OriginalString)}/driveItem";

        for (int attempt = 0; attempt < 5; attempt++)
        {
            using HttpResponseMessage response = await SendAsync(http, HttpMethod.Get, url).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // Only a 404 is worth waiting out. A 401 or 403 is a grant problem that no amount of waiting fixes, and
            // retrying it would turn a clear diagnosis into a slow one.
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                UnavailableReason = $"the item did not resolve through /shares: {(int)response.StatusCode} {await BodyAsync(response).ConfigureAwait(false)}";
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        UnavailableReason = "the item never resolved through /shares within 10 seconds";
    }

    public async Task DisposeAsync()
    {
        if (_seededDriveId is null || _seededItemId is null)
        {
            return;
        }

        string note;

        try
        {
            using HttpClient http = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await SendAsync(http, HttpMethod.Delete, $"{Root}/v1.0/drives/{_seededDriveId}/items/{_seededItemId}").ConfigureAwait(false);

            // A stray file is a nuisance, not a failure, and throwing here would mask the real test result. The fixed
            // name prefix is what makes strays findable if cleanup ever stops working.
            note = $"cleanup of seeded item: {(int)response.StatusCode}";
        }
#pragma warning disable CA1031 // cleanup must never fail the run
        catch (Exception ex)
#pragma warning restore CA1031
        {
            note = $"cleanup of seeded item threw: {ex.Message}";
        }

        // Best-effort, and it must be the last thing that happens. DisposeAsync runs after the class's last test has
        // completed, so xUnit's ITestOutputHelper has no active test to attach to and throws
        // "There is no currently active test". Writing the cleanup note through it would then escape DisposeAsync as a
        // Test Class Cleanup Failure and fail the whole run even when every test passed, which is exactly the outcome
        // this fixture's "cleanup must never fail the run" intent forbids.
        try
        {
            OutputHelper?.WriteLine(note);
        }
#pragma warning disable CA1031 // logging the cleanup outcome must never fail the run
        catch (Exception)
#pragma warning restore CA1031
        {
        }
    }

    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private string Root => GraphRoot.ToString().TrimEnd('/');

    private async Task<HttpResponseMessage> SendAsync(HttpClient http, HttpMethod method, string url)
    {
        using HttpRequestMessage request = new(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AgentToken);

        return await http.SendAsync(request).ConfigureAwait(false);
    }

    private static async Task<string> BodyAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return body.Length > 512 ? body[..512] : body;
    }

    /// <summary>
    /// Graph's published encoding, transcribed from its own C# sample so the fixture's readiness probe does not
    /// depend on the code under test. If this and <c>GraphShare.EncodeSharingUrl</c> ever disagree, the probe passes
    /// and the assertion fails, which is the direction that points at the right file.
    /// </summary>
    internal static string EncodeSharingUrl(string url)
        => "u!" + Convert.ToBase64String(Encoding.UTF8.GetBytes(url)).TrimEnd('=').Replace('/', '_').Replace('+', '-');

    /// <summary>Roles and scopes carried by a JWT, decoded independently of the SDK. Empty when it does not decode.</summary>
    private static IReadOnlyList<string> PermissionsOf(string token)
    {
        try
        {
            string[] segments = token.Split('.');

            if (segments.Length < 2)
            {
                return [];
            }

            string padded = segments[1].Replace('-', '+').Replace('_', '/');
            byte[] bytes = Convert.FromBase64String(padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '='));

            using JsonDocument claims = JsonDocument.Parse(bytes);
            List<string> permissions = [];

            if (claims.RootElement.TryGetProperty("roles", out JsonElement roles) && roles.ValueKind == JsonValueKind.Array)
            {
                permissions.AddRange(roles.EnumerateArray().Where(r => r.ValueKind == JsonValueKind.String).Select(r => r.GetString()!));
            }

            if (claims.RootElement.TryGetProperty("scp", out JsonElement scp) && scp.ValueKind == JsonValueKind.String)
            {
                permissions.AddRange(scp.GetString()!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            }

            return permissions;
        }
#pragma warning disable CA1031 // an undecodable token is reported as "no permissions", which skips rather than fails
        catch (Exception)
#pragma warning restore CA1031
        {
            return [];
        }
    }
}
