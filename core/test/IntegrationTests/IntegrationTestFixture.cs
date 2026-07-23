// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Shared fixture that configures DI, acquires tokens, and exposes clients for integration tests.
/// Reused across test classes via IClassFixture to avoid repeated token acquisition.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime, IDisposable, ITestOutputHelperAccessor
{
    public ServiceProvider ServiceProvider { get; }
    public ConversationClient ConversationClient { get; }
    public ApiClient ApiClient { get; }

    public Uri ServiceUrl { get; }
    public string ConversationId { get; }
    public string UserId { get; }
    public string TeamId { get; }
    public string ChannelId { get; }
    public string MeetingId { get; }
    public string TenantId { get; }
    public string BotAppId { get; }
    public string? UserId2 { get; }
    public AgenticIdentity? AgenticIdentity { get; }

    /// <summary>
    /// True when running against the canary service endpoint.
    /// </summary>
    public bool IsCanary => ServiceUrl.Host.Contains("canary", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Cached conversation members — fetched once during InitializeAsync to avoid
    /// repeated /members calls that trigger throttling (429).
    /// </summary>
    public IList<TeamsChannelAccount?>? CachedMembers { get; private set; }

    /// <summary>
    /// First member MRI from cache (convenience for tests needing a valid member ID).
    /// </summary>
    public string? MemberMri1 => CachedMembers?.FirstOrDefault()?.Id;

    /// <summary>
    /// Second member MRI from cache (for group chat tests requiring 2+ members).
    /// </summary>
    public string? MemberMri2 => CachedMembers?.Skip(1).FirstOrDefault()?.Id;

    /// <summary>
    /// Third member MRI from cache.
    /// </summary>
    public string? MemberMri3 => CachedMembers?.Skip(2).FirstOrDefault()?.Id;

    /// <summary>
    /// Set by each test class constructor to route ILogger output to xUnit's test output.
    /// </summary>
    public ITestOutputHelper? OutputHelper { get; set; }

    public IntegrationTestFixture()
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
        ConversationClient = ServiceProvider.GetRequiredService<ConversationClient>();
        ApiClient = ServiceProvider.GetRequiredService<ApiClient>();

        ServiceUrl = new Uri(Env("TEST_SERVICEURL", "https://smba.trafficmanager.net/teams/"));
        ConversationId = Env("TEST_CONVERSATIONID");
        UserId = Env("TEST_USER_ID");
        TeamId = Env("TEST_TEAMID");
        ChannelId = Env("TEST_CHANNELID");
        MeetingId = Env("TEST_MEETINGID");
        TenantId = Env("TEST_TENANTID");
        BotAppId = Env("AzureAd__ClientId");
        UserId2 = Environment.GetEnvironmentVariable("TEST_USER_ID_2");

        string? agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
        string? agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");

        if (!string.IsNullOrEmpty(agenticAppId) && !string.IsNullOrEmpty(agenticUserId))
        {
            string appBlueprintId = Env("AzureAd__ClientId");
            ChannelAccount recipient = new()
            {
                AgenticAppBlueprintId = appBlueprintId,
                AgenticAppId = agenticAppId,
                AgenticUserId = agenticUserId
            };
            AgenticIdentity = AgenticIdentity.FromAccount(recipient);
        }
    }

    /// <summary>
    /// Fetches and caches conversation members once for the entire test run.
    /// Filters out the bot itself and null entries. Fails fast if no usable members are found.
    /// </summary>
    public async Task InitializeAsync()
    {
        ApiClient scoped = ScopedApiClient;
        IList<TeamsChannelAccount?> raw = await scoped.Conversations.GetMembersAsync(ConversationId);

        string botMri = $"28:{BotAppId}";
        CachedMembers = raw
            .Where(m => m?.Id is not null && !m.Id.Equals(botMri, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (CachedMembers.Count == 0)
        {
            throw new InvalidOperationException(
                "No usable members found in test conversation (all null or bot-only). " +
                "Ensure the conversation has at least 2 non-bot members.");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public ApiClient ScopedApiClient => ApiClient.ForServiceUrl(ServiceUrl).ForAgenticIdentity(AgenticIdentity);

    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string Env(string name, string? fallback = null) =>
        Environment.GetEnvironmentVariable(name)
        ?? fallback
        ?? throw new InvalidOperationException($"{name} environment variable not set");

    internal static ChannelAccount GetChannelAccountWithAgenticProperties()
    {
        string agenticUserId = Env("TEST_AGENTIC_USERID");
        string agenticAppId = Env("TEST_AGENTIC_APPID");
        string agenticAppBlueprintId = Env("AzureAd__ClientId");

        if (string.IsNullOrEmpty(agenticUserId))
        {
            return new ChannelAccount();
        }

        ChannelAccount account = new()
        {
            Id = agenticUserId,
            Name = "Agentic User",
            AgenticAppBlueprintId = agenticAppBlueprintId,
            AgenticAppId = agenticAppId,
            AgenticUserId = agenticUserId
        };
        return account;
    }

    internal static AgenticIdentity GetAgenticIdentity()
    {
        string agenticUserId = Env("TEST_AGENTIC_USERID");
        string agenticAppId = Env("TEST_AGENTIC_APPID");
        string agenticAppBlueprintId = Env("AzureAd__ClientId");

        if (string.IsNullOrEmpty(agenticUserId))
        {
            return null!;
        }

        AgenticIdentity identity = new()
        {
            AgenticUserId = agenticUserId,
            AgenticAppId = agenticAppId,
            AgenticAppBlueprintId = agenticAppBlueprintId
        };
        return identity;
    }
}
