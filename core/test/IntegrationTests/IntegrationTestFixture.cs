// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Api.Clients;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Shared fixture that configures DI, acquires tokens, and exposes clients for integration tests.
/// Reused across test classes via IClassFixture to avoid repeated token acquisition.
/// </summary>
public class IntegrationTestFixture : IDisposable, ITestOutputHelperAccessor
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
            ConversationAccount recipient = new()
            {
                AgenticAppBlueprintId = appBlueprintId,
                AgenticAppId = agenticAppId,
                AgenticUserId = agenticUserId
            };
            AgenticIdentity = AgenticIdentity.FromAccount(recipient);
        }
    }

    public ApiClient ScopedApiClient => ApiClient.ForServiceUrl(ServiceUrl);

    public void Dispose()
    {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string Env(string name, string? fallback = null) =>
        Environment.GetEnvironmentVariable(name)
        ?? fallback
        ?? throw new InvalidOperationException($"{name} environment variable not set");

    internal static ConversationAccount GetConversationAccountWithAgenticProperties()
    {
        var agenticUserId = Env("TEST_AGENTIC_USERID");
        var agenticAppId = Env("TEST_AGENTIC_APPID");
        var agenticAppBlueprintId = Env("AzureAd__ClientId");

        if (string.IsNullOrEmpty(agenticUserId))
        {
            return new ConversationAccount();
        }

        ConversationAccount account = new()
        {
            Id = agenticUserId,
            Name = "Agentic User",
            Properties =
            {
                { "agenticUserId", agenticUserId },
                { "agenticAppId", agenticAppId },
                { "agenticAppBlueprintId", agenticAppBlueprintId }
            }
        };
        return account;
    }

    internal static AgenticIdentity GetAgenticIdentity()
    {
        var agenticUserId = Env("TEST_AGENTIC_USERID");
        var agenticAppId = Env("TEST_AGENTIC_APPID");
        var agenticAppBlueprintId = Env("AzureAd__ClientId");

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
