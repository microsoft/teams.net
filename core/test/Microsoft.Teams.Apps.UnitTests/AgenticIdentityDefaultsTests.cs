// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class AgenticIdentityDefaultsTests
{
    private static readonly Uri ServiceUrl = new("https://smba.trafficmanager.net/amer/");

    [Fact]
    public async Task ContextApi_UsesRecipientAgenticIdentityAsDefault()
    {
        CapturingHttpMessageHandler handler = new();
        TeamsBotApplication app = CreateApp(handler);
        AgenticIdentity expected = DefaultIdentity();
        Context<TeamsActivity> context = new(
            app,
            new TeamsActivity
            {
                Type = TeamsActivityTypes.Message,
                ServiceUrl = ServiceUrl,
                Recipient = new TeamsChannelAccount
                {
                    AgenticAppId = expected.AgenticAppId,
                    AgenticUserId = expected.AgenticUserId,
                    AgenticAppBlueprintId = expected.AgenticAppBlueprintId,
                    TenantId = expected.TenantId
                }
            });

        await context.Api.Conversations.GetMembersPagedAsync("conversation-id");

        CapturedRequest request = Assert.Single(handler.Requests);
        AssertIdentity(expected, request.AgenticIdentity);
    }

    [Fact]
    public async Task ContextApi_ExplicitIdentityOverridesRecipientDefault()
    {
        CapturingHttpMessageHandler handler = new();
        TeamsBotApplication app = CreateApp(handler);
        AgenticIdentity explicitIdentity = ExplicitIdentity();
        Context<TeamsActivity> context = new(
            app,
            new TeamsActivity
            {
                Type = TeamsActivityTypes.Message,
                ServiceUrl = ServiceUrl,
                Recipient = new TeamsChannelAccount
                {
                    AgenticAppId = "default-agentic-app-id",
                    AgenticUserId = "default-agentic-user-id",
                    AgenticAppBlueprintId = "default-agentic-blueprint-id",
                    TenantId = "default-tenant-id"
                }
            });

        await context.Api.Conversations.GetMemberByIdAsync("conversation-id", "member-id", explicitIdentity);

        CapturedRequest request = Assert.Single(handler.Requests);
        AssertIdentity(explicitIdentity, request.AgenticIdentity);
    }

    [Fact]
    public async Task ScopedApi_ServiceUrlBoundClientsUseDefaultIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity expected = DefaultIdentity();
        ApiClient api = CreateApiClient(handler).ForServiceUrl(ServiceUrl, expected);

        await api.Conversations.CreateAsync(new ConversationParameters());
        await api.Conversations.GetMemberByIdAsync("conversation-id", "member-id");
        await api.Conversations.AddReactionAsync("conversation-id", "activity-id", "like");
        await api.Teams.GetByIdAsync("team-id");
        await api.Meetings.GetByIdAsync("meeting-id");

        Assert.Equal(5, handler.Requests.Count);
        Assert.All(handler.Requests, request => AssertIdentity(expected, request.AgenticIdentity));
    }

    [Fact]
    public async Task ScopedApi_ExplicitIdentityOverridesDefaultIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity explicitIdentity = ExplicitIdentity();
        ApiClient api = CreateApiClient(handler).ForServiceUrl(ServiceUrl, DefaultIdentity());

        await api.Meetings.GetParticipantAsync("meeting-id", "participant-id", "tenant-id", explicitIdentity);

        CapturedRequest request = Assert.Single(handler.Requests);
        AssertIdentity(explicitIdentity, request.AgenticIdentity);
    }

    [Fact]
    public async Task ForInboundActivity_UsesServiceUrlAndRecipientAgenticIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity expected = DefaultIdentity();
        ApiClient api = CreateApiClient(handler).ForInboundActivity(new TeamsActivity
        {
            ServiceUrl = ServiceUrl,
            Recipient = new TeamsChannelAccount
            {
                AgenticAppId = expected.AgenticAppId,
                AgenticUserId = expected.AgenticUserId,
                AgenticAppBlueprintId = expected.AgenticAppBlueprintId,
                TenantId = expected.TenantId
            }
        });

        await api.Conversations.GetMembersPagedAsync("conversation-id");

        Assert.Equal(ServiceUrl, api.ServiceUrl);
        CapturedRequest request = Assert.Single(handler.Requests);
        AssertIdentity(expected, request.AgenticIdentity);
    }

    [Fact]
    public async Task ForRequestOptions_UsesServiceUrlAndAgenticIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity expected = DefaultIdentity();
        ApiClient api = CreateApiClient(handler).ForRequestOptions(new RequestOptions
        {
            ServiceUrl = ServiceUrl,
            AgenticIdentity = expected
        });

        await api.Conversations.GetMembersPagedAsync("conversation-id");

        Assert.Equal(ServiceUrl, api.ServiceUrl);
        CapturedRequest request = Assert.Single(handler.Requests);
        AssertIdentity(expected, request.AgenticIdentity);
    }

    [Fact]
    public void ForInboundActivity_ThrowsWhenServiceUrlIsMissing()
    {
        ApiClient api = CreateApiClient(new CapturingHttpMessageHandler());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => api.ForInboundActivity(new TeamsActivity()));

        Assert.Equal("Activity.ServiceUrl is required to use the Api client.", exception.Message);
    }

    [Fact]
    public void ForRequestOptions_ThrowsWhenServiceUrlIsMissing()
    {
        ApiClient api = CreateApiClient(new CapturingHttpMessageHandler());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => api.ForRequestOptions(new RequestOptions()));

        Assert.Equal("RequestOptions.ServiceUrl is required to scope the Api client.", exception.Message);
    }

    [Fact]
    public async Task ConversationApi_ActivityMethodsUseDefaultIdentityWhenExplicitIdentityIsMissing()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity expected = DefaultIdentity();
        ConversationApiClient conversations = CreateApiClient(handler).ForServiceUrl(ServiceUrl, expected).Conversations;

        await conversations.CreateActivityAsync("conversation-id", CreateActivity());
        await conversations.UpdateActivityAsync("conversation-id", "activity-id", CreateActivity());
        await conversations.ReplyToActivityAsync("conversation-id", "activity-id", CreateActivity());
        await conversations.DeleteActivityAsync("conversation-id", "activity-id");
        await conversations.GetActivityMembersAsync("conversation-id", "activity-id");
        await conversations.CreateTargetedActivityAsync("conversation-id", CreateTargetedActivity());
        await conversations.UpdateTargetedActivityAsync("conversation-id", "activity-id", CreateActivity());
        await conversations.DeleteTargetedActivityAsync("conversation-id", "activity-id");

        Assert.Equal(8, handler.Requests.Count);
        Assert.All(handler.Requests, request => AssertIdentity(expected, request.AgenticIdentity));
    }

    [Fact]
    public async Task ConversationApi_ActivityMethodsExplicitIdentityOverridesDefaultIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity explicitIdentity = ExplicitIdentity();
        ConversationApiClient conversations = CreateApiClient(handler).ForServiceUrl(ServiceUrl, DefaultIdentity()).Conversations;
        RequestOptions options = CreateRequestOptions(explicitIdentity);

        await conversations.CreateActivityAsync("conversation-id", CreateActivity(), options);
        await conversations.UpdateActivityAsync("conversation-id", "activity-id", CreateActivity(), options);
        await conversations.ReplyToActivityAsync("conversation-id", "activity-id", CreateActivity(), options);
        await conversations.DeleteActivityAsync("conversation-id", "activity-id", options);
        await conversations.GetActivityMembersAsync("conversation-id", "activity-id", options);
        await conversations.CreateTargetedActivityAsync("conversation-id", CreateTargetedActivity(), options);
        await conversations.UpdateTargetedActivityAsync("conversation-id", "activity-id", CreateActivity(), options);
        await conversations.DeleteTargetedActivityAsync("conversation-id", "activity-id", options);

        Assert.Equal(8, handler.Requests.Count);
        Assert.All(handler.Requests, request => AssertIdentity(explicitIdentity, request.AgenticIdentity));
    }

    [Fact]
    public async Task ConversationApi_ActivityFromIdentityOverridesFallbackIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity fromIdentity = new()
        {
            AgenticAppId = "from-agentic-app-id",
            AgenticUserId = "from-agentic-user-id",
            AgenticAppBlueprintId = "from-agentic-blueprint-id",
            TenantId = "from-tenant-id"
        };
        ConversationApiClient conversations = CreateApiClient(handler).ForServiceUrl(ServiceUrl, DefaultIdentity()).Conversations;
        RequestOptions options = CreateRequestOptions(ExplicitIdentity());

        await conversations.CreateActivityAsync("conversation-id", CreateActivity(fromIdentity), options);
        await conversations.UpdateActivityAsync("conversation-id", "activity-id", CreateActivity(fromIdentity), options);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            AssertIdentity(fromIdentity, request.AgenticIdentity);
            Assert.Equal("from-bot-id", request.BotAppId);
        });
    }

    [Fact]
    public async Task ConversationApi_ActivityRequestOptionsServiceUrlOverridesScopedServiceUrl()
    {
        CapturingHttpMessageHandler handler = new();
        Uri serviceUrl = new("https://override.example.com/");
        ConversationApiClient conversations = CreateApiClient(handler).ForServiceUrl(ServiceUrl, DefaultIdentity()).Conversations;

        await conversations.CreateActivityAsync("conversation-id", CreateActivity(), new RequestOptions { ServiceUrl = serviceUrl });

        CapturedRequest request = Assert.Single(handler.Requests);
        Assert.StartsWith(serviceUrl.ToString(), request.Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ActivityClient_UsesDefaultIdentityWhenExplicitIdentityIsMissing()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity expected = DefaultIdentity();
#pragma warning disable CS0618 // Verifies backward-compatible obsolete wrapper behavior.
        ActivityClient activities = CreateApiClient(handler).ForServiceUrl(ServiceUrl, expected).Conversations.Activities;
#pragma warning restore CS0618

        await activities.CreateAsync("conversation-id", CreateActivity());
        await activities.UpdateAsync("conversation-id", "activity-id", CreateActivity());
        await activities.ReplyAsync("conversation-id", "activity-id", CreateActivity());
        await activities.DeleteAsync("conversation-id", "activity-id");
        await activities.GetMembersAsync("conversation-id", "activity-id");
        await activities.CreateTargetedAsync("conversation-id", CreateTargetedActivity());
        await activities.UpdateTargetedAsync("conversation-id", "activity-id", CreateActivity());
        await activities.DeleteTargetedAsync("conversation-id", "activity-id");

        Assert.Equal(8, handler.Requests.Count);
        Assert.All(handler.Requests, request => AssertIdentity(expected, request.AgenticIdentity));
    }

    [Fact]
    public async Task ActivityClient_ExplicitIdentityOverridesDefaultIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity explicitIdentity = ExplicitIdentity();
#pragma warning disable CS0618 // Verifies backward-compatible obsolete wrapper behavior.
        ActivityClient activities = CreateApiClient(handler).ForServiceUrl(ServiceUrl, DefaultIdentity()).Conversations.Activities;
#pragma warning restore CS0618
        RequestOptions options = CreateRequestOptions(explicitIdentity);

        await activities.CreateAsync("conversation-id", CreateActivity(), options);
        await activities.UpdateAsync("conversation-id", "activity-id", CreateActivity(), options);
        await activities.ReplyAsync("conversation-id", "activity-id", CreateActivity(), options);
        await activities.DeleteAsync("conversation-id", "activity-id", options);
        await activities.GetMembersAsync("conversation-id", "activity-id", options);
        await activities.CreateTargetedAsync("conversation-id", CreateTargetedActivity(), options);
        await activities.UpdateTargetedAsync("conversation-id", "activity-id", CreateActivity(), options);
        await activities.DeleteTargetedAsync("conversation-id", "activity-id", options);

        Assert.Equal(8, handler.Requests.Count);
        Assert.All(handler.Requests, request => AssertIdentity(explicitIdentity, request.AgenticIdentity));
    }

    [Fact]
    public async Task ActivityClient_ActivityFromIdentityOverridesFallbackIdentity()
    {
        CapturingHttpMessageHandler handler = new();
        AgenticIdentity fromIdentity = new()
        {
            AgenticAppId = "from-agentic-app-id",
            AgenticUserId = "from-agentic-user-id",
            AgenticAppBlueprintId = "from-agentic-blueprint-id",
            TenantId = "from-tenant-id"
        };
#pragma warning disable CS0618 // Verifies backward-compatible obsolete wrapper behavior.
        ActivityClient activities = CreateApiClient(handler).ForServiceUrl(ServiceUrl, DefaultIdentity()).Conversations.Activities;
#pragma warning restore CS0618
        RequestOptions options = CreateRequestOptions(ExplicitIdentity());

        await activities.CreateAsync("conversation-id", CreateActivity(fromIdentity), options);
        await activities.UpdateAsync("conversation-id", "activity-id", CreateActivity(fromIdentity), options);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            AssertIdentity(fromIdentity, request.AgenticIdentity);
            Assert.Equal("from-bot-id", request.BotAppId);
        });
    }

    [Fact]
    public void GetAgenticIdentity_DefaultsBlueprintAndTenant()
    {
        TeamsBotApplication app = CreateApp(options: new TeamsBotApplicationOptions
        {
            AppId = "app-id",
            TenantId = "tenant-id"
        });

        AgenticIdentity identity = app.GetAgenticIdentity("agentic-app-id", "agentic-user-id");

        Assert.Equal("agentic-app-id", identity.AgenticAppId);
        Assert.Equal("agentic-user-id", identity.AgenticUserId);
        Assert.Equal("app-id", identity.AgenticAppBlueprintId);
        Assert.Equal("tenant-id", identity.TenantId);
    }

    [Fact]
    public void GetAgenticIdentity_ExplicitValuesOverrideDefaults()
    {
        TeamsBotApplication app = CreateApp(options: new TeamsBotApplicationOptions
        {
            AppId = "app-id",
            TenantId = "tenant-id"
        });

        AgenticIdentity identity = app.GetAgenticIdentity(
            "agentic-app-id",
            "agentic-user-id",
            tenantId: "explicit-tenant-id",
            agenticAppBlueprintId: "explicit-blueprint-id");

        Assert.Equal("explicit-blueprint-id", identity.AgenticAppBlueprintId);
        Assert.Equal("explicit-tenant-id", identity.TenantId);
    }

    [Fact]
    public void GetAgenticIdentity_LeavesTenantNullWhenUnconfigured()
    {
        TeamsBotApplication app = CreateApp(options: new TeamsBotApplicationOptions { AppId = "app-id" });

        AgenticIdentity identity = app.GetAgenticIdentity("agentic-app-id", "agentic-user-id");

        Assert.Equal("app-id", identity.AgenticAppBlueprintId);
        Assert.Null(identity.TenantId);
    }

    private static TeamsBotApplication CreateApp(CapturingHttpMessageHandler? handler = null, TeamsBotApplicationOptions? options = null)
        => new(
            CreateApiClient(handler ?? new CapturingHttpMessageHandler()),
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            options ?? new TeamsBotApplicationOptions { AppId = "app-id" });

    private static ApiClient CreateApiClient(CapturingHttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler);
        ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);
        UserTokenClient userTokenClient = new(new HttpClient(), new ConfigurationBuilder().Build(), NullLogger<UserTokenClient>.Instance);
        return new ApiClient(httpClient, conversationClient, userTokenClient);
    }

    private static CoreActivity CreateActivity(AgenticIdentity? fromIdentity = null)
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message
        };

        if (fromIdentity is not null)
        {
            activity.From = new ChannelAccount
            {
                Id = "28:from-bot-id",
                AgenticAppId = fromIdentity.AgenticAppId,
                AgenticUserId = fromIdentity.AgenticUserId,
                AgenticAppBlueprintId = fromIdentity.AgenticAppBlueprintId,
                TenantId = fromIdentity.TenantId
            };
        }

        return activity;
    }

    private static CoreActivity CreateTargetedActivity()
        => new()
        {
            Type = ActivityType.Message,
            Recipient = new ChannelAccount { Id = "recipient-id" }
        };

    private static AgenticIdentity DefaultIdentity()
        => new()
        {
            AgenticAppId = "default-agentic-app-id",
            AgenticUserId = "default-agentic-user-id",
            AgenticAppBlueprintId = "default-agentic-blueprint-id",
            TenantId = "default-tenant-id"
        };

    private static AgenticIdentity ExplicitIdentity()
        => new()
        {
            AgenticAppId = "explicit-agentic-app-id",
            AgenticUserId = "explicit-agentic-user-id",
            AgenticAppBlueprintId = "explicit-agentic-blueprint-id",
            TenantId = "explicit-tenant-id"
        };

    private static RequestOptions CreateRequestOptions(AgenticIdentity agenticIdentity)
        => new() { AgenticIdentity = agenticIdentity };

    private static void AssertIdentity(AgenticIdentity expected, AgenticIdentity? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.AgenticAppId, actual!.AgenticAppId);
        Assert.Equal(expected.AgenticUserId, actual.AgenticUserId);
        Assert.Equal(expected.AgenticAppBlueprintId, actual.AgenticAppBlueprintId);
        Assert.Equal(expected.TenantId, actual.TenantId);
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private static readonly HttpRequestOptionsKey<object?> AgenticIdentityKey = new(BotRequestContext.AgenticIdentityKey);
        private static readonly HttpRequestOptionsKey<object?> BotAppIdKey = new(BotRequestContext.BotAppIdKey);

        public List<CapturedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Options.TryGetValue(AgenticIdentityKey, out object? agenticIdentity);
            request.Options.TryGetValue(BotAppIdKey, out object? botAppId);
            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                agenticIdentity as AgenticIdentity,
                botAppId as string));

            return Task.FromResult(CreateResponse(request));
        }

        private static HttpResponseMessage CreateResponse(HttpRequestMessage request)
        {
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;
            string json = path.Contains("/pagedmembers", StringComparison.Ordinal)
                ? "{\"members\":[{\"id\":\"member-id\"}]}"
                : path.EndsWith("/members", StringComparison.Ordinal)
                    ? "[{\"id\":\"member-id\"}]"
                    : path.Contains("/members/", StringComparison.Ordinal)
                        ? "{\"id\":\"member-id\"}"
                        : path.EndsWith("/conversations", StringComparison.Ordinal) && request.Method == HttpMethod.Post
                            ? "{\"id\":\"conversation-id\"}"
                            : path.Contains("/activities", StringComparison.Ordinal) && request.Method != HttpMethod.Delete
                                ? "{\"id\":\"activity-id\"}"
                                : path.Contains("/teams/", StringComparison.Ordinal) && path.EndsWith("/conversations", StringComparison.Ordinal)
                                    ? "{\"conversations\":[]}"
                                    : path.Contains("/teams/", StringComparison.Ordinal)
                                        ? "{\"id\":\"team-id\"}"
                                        : path.Contains("/meetings/", StringComparison.Ordinal) && path.Contains("/participants/", StringComparison.Ordinal)
                                            ? "{\"user\":{\"id\":\"participant-id\"}}"
                                            : path.Contains("/meetings/", StringComparison.Ordinal)
                                                ? "{\"id\":\"meeting-id\"}"
                                                : string.Empty;

            HttpResponseMessage response = new(HttpStatusCode.OK);
            if (json.Length > 0)
            {
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return response;
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, string Url, AgenticIdentity? AgenticIdentity, string? BotAppId);
}
