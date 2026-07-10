// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class AgentLifecycleHandlerTests
{
    private const string TenantId = "00000000-0000-0000-0000-000000000001";
    private const string AgenticUserId = "00000000-0000-0000-0000-000000000002";
    private const string AppId = "00000000-0000-0000-0000-000000000003";
    private const string AgenticAppInstanceId = "00000000-0000-0000-0000-000000000004";
    private const string BlueprintId = "00000000-0000-0000-0000-000000000005";
    private const string ManagerId = "3c22b565-74f3-48b0-aa18-1dc03b8ec270";

    public static TheoryData<string, Type> VariantTypes => new()
    {
        { AgentLifecycleEventValueTypes.AgenticUserIdentityCreated, typeof(AgenticUserIdentityCreatedActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated, typeof(AgenticUserIdentityUpdatedActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserManagerUpdated, typeof(AgenticUserManagerUpdatedActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserEnabled, typeof(AgenticUserEnabledActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserDisabled, typeof(AgenticUserDisabledActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserDeleted, typeof(AgenticUserDeletedActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserUndeleted, typeof(AgenticUserUndeletedActivity) },
        { AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated, typeof(AgenticUserWorkloadOnboardingUpdatedActivity) },
    };

    [Theory]
    [MemberData(nameof(VariantTypes))]
    public void FromActivity_DiscriminatesAgentLifecycleVariants(string valueType, Type expectedType)
    {
        TeamsActivity activity = ParseLifecycleActivity(valueType);

        Assert.Equal(expectedType, activity.GetType());

        EventActivity eventActivity = Assert.IsAssignableFrom<EventActivity>(activity);
        Assert.Equal(TeamsActivityTypes.Event, eventActivity.Type);
        Assert.Equal(EventNames.AgentLifecycle, eventActivity.Name);
        Assert.Equal(valueType, eventActivity.ValueType);
        Assert.Equal(TenantId, eventActivity.ChannelData?.Tenant?.Id);
        AssertLifecycleValue(activity, valueType);
    }

    [Fact]
    public void FromActivity_KeepsUnknownAgentLifecycleValueTypeAsBaseLifecycleActivity()
    {
        TeamsActivity activity = ParseLifecycleActivity("UnknownLifecycleValueType", CommonValue("unknownLifecycleEvent"));

        AgentLifecycleEventActivity lifecycleActivity = Assert.IsType<AgentLifecycleEventActivity>(activity);
        Assert.Equal(EventNames.AgentLifecycle, lifecycleActivity.Name);
        Assert.Equal("UnknownLifecycleValueType", lifecycleActivity.ValueType);
        Assert.NotNull(lifecycleActivity.Value);
    }

    [Fact]
    public void ToJson_PreservesAgentLifecycleEnvelope()
    {
        AgenticUserManagerUpdatedActivity activity = Assert.IsType<AgenticUserManagerUpdatedActivity>(
            ParseLifecycleActivity(AgentLifecycleEventValueTypes.AgenticUserManagerUpdated));

        string json = activity.ToJson();
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal(1, CountOccurrences(json, "\"valueType\""));
        Assert.Equal(1, CountOccurrences(json, "\"channelData\""));
        Assert.Equal(TeamsActivityTypes.Event, root.GetProperty("type").GetString());
        Assert.Equal(EventNames.AgentLifecycle, root.GetProperty("name").GetString());
        Assert.Equal(AgentLifecycleEventValueTypes.AgenticUserManagerUpdated, root.GetProperty("valueType").GetString());
        Assert.Equal(AgenticAppInstanceId, root.GetProperty("value").GetProperty("agenticAppInstanceId").GetString());
        Assert.Equal(ManagerId, root.GetProperty("value").GetProperty("manager").GetProperty("managerId").GetString());
    }

    [Fact]
    public async Task OnEvent_StillReceivesAgentLifecycleEvents()
    {
        TeamsBotApplication app = CreateApp();
        bool called = false;

        app.OnEvent((ctx, _) =>
        {
            called = true;
            Assert.IsType<AgenticUserEnabledActivity>(ctx.Activity);
            Assert.Equal(EventNames.AgentLifecycle, ctx.Activity.Name);
            Assert.Equal(AgentLifecycleEventValueTypes.AgenticUserEnabled, ctx.Activity.ValueType);
            return Task.CompletedTask;
        });

        await DispatchAsync(app, AgentLifecycleEventValueTypes.AgenticUserEnabled);

        Assert.True(called);
    }

    [Theory]
    [MemberData(nameof(VariantTypes))]
    public async Task VariantHandlers_DispatchTypedContextAndPropagateState(string valueType, Type expectedType)
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer state = CreateState();
        int lifecycleHandlerCalls = 0;
        int variantHandlerCalls = 0;

        app.OnAgentLifecycle((ctx, _) =>
        {
            lifecycleHandlerCalls++;
            Assert.Same(state, ctx.State);
            Assert.Equal(valueType, ctx.Activity.ValueType);
            Assert.Equal(expectedType, AgentLifecycleEventActivity.FromEventActivity(ctx.Activity).GetType());
            return Task.CompletedTask;
        });

        RegisterVariantHandler(app, valueType, expectedType, state, () => variantHandlerCalls++);

        await DispatchAsync(app, valueType, state);

        Assert.Equal(1, lifecycleHandlerCalls);
        Assert.Equal(1, variantHandlerCalls);
    }

    [Fact]
    public async Task VariantHandler_DoesNotDispatchForDifferentValueType()
    {
        TeamsBotApplication app = CreateApp();
        bool called = false;

        app.OnAgenticUserEnabled((_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await DispatchAsync(app, AgentLifecycleEventValueTypes.AgenticUserDisabled);

        Assert.False(called);
    }

    private static void RegisterVariantHandler(
        TeamsBotApplication app,
        string valueType,
        Type expectedType,
        TurnStateContainer state,
        Action onCalled)
    {
        switch (valueType)
        {
            case AgentLifecycleEventValueTypes.AgenticUserIdentityCreated:
                app.OnAgenticUserIdentityCreated((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal(ManagerId, ctx.Activity.Value?.Manager?.UserId);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated:
                app.OnAgenticUserIdentityUpdated((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal("Mail", ctx.Activity.Value?.UpdatedProperty.PropertyName);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserManagerUpdated:
                app.OnAgenticUserManagerUpdated((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal(ManagerId, ctx.Activity.Value?.Manager?.ManagerId);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserEnabled:
                app.OnAgenticUserEnabled((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal(6, ctx.Activity.Value?.Version);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserDisabled:
                app.OnAgenticUserDisabled((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal(7, ctx.Activity.Value?.Version);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserDeleted:
                app.OnAgenticUserDeleted((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal("UserSoftDelete", ctx.Activity.Value?.DeletionReason);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserUndeleted:
                app.OnAgenticUserUndeleted((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal(9, ctx.Activity.Value?.Version);
                    return Task.CompletedTask;
                });
                break;

            case AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated:
                app.OnAgenticUserWorkloadOnboardingUpdated((ctx, _) =>
                {
                    AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                    Assert.Equal("Teams", ctx.Activity.Value?.WorkloadName);
                    Assert.Equal("succeeded", ctx.Activity.Value?.WorkloadOnboardingState);
                    return Task.CompletedTask;
                });
                break;
        }
    }

    private static void AssertVariantContext<TActivity>(
        Context<TActivity> context,
        Type expectedType,
        string valueType,
        TurnStateContainer state,
        Action onCalled) where TActivity : AgentLifecycleEventActivity
    {
        onCalled();
        Assert.Same(state, context.State);
        Assert.Equal(expectedType, context.Activity.GetType());
        Assert.Equal(EventNames.AgentLifecycle, context.Activity.Name);
        Assert.Equal(valueType, context.Activity.ValueType);
    }

    private static void AssertLifecycleValue(TeamsActivity activity, string valueType)
    {
        switch (valueType)
        {
            case AgentLifecycleEventValueTypes.AgenticUserIdentityCreated:
                AgenticUserIdentityCreatedActivity created = Assert.IsType<AgenticUserIdentityCreatedActivity>(activity);
                Assert.Equal(AgentLifecycleEventTypes.AgenticUserIdentityCreated, created.Value?.EventType);
                Assert.Equal(AgenticUserId, created.Value?.AgenticUserId);
                Assert.Equal(ManagerId, created.Value?.Manager?.UserId);
                Assert.Equal("manager@example.test", created.Value?.Manager?.Email);
                Assert.Equal(1, created.Value?.ExpirationDateTime?.Year);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated:
                AgenticUserIdentityUpdatedActivity updated = Assert.IsType<AgenticUserIdentityUpdatedActivity>(activity);
                Assert.Equal("Mail", updated.Value?.UpdatedProperty.PropertyName);
                Assert.Equal("newinstance4@teamssdk.onmicrosoft.com", updated.Value?.UpdatedProperty.PropertyValue);
                Assert.Equal(4, updated.Value?.Version);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserManagerUpdated:
                AgenticUserManagerUpdatedActivity managerUpdated = Assert.IsType<AgenticUserManagerUpdatedActivity>(activity);
                Assert.Equal(ManagerId, managerUpdated.Value?.Manager?.ManagerId);
                Assert.Equal(6, managerUpdated.Value?.Version);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserEnabled:
                AgenticUserEnabledActivity enabled = Assert.IsType<AgenticUserEnabledActivity>(activity);
                Assert.Equal(AgentLifecycleEventTypes.AgenticUserEnabled, enabled.Value?.EventType);
                Assert.Equal(6, enabled.Value?.Version);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserDisabled:
                AgenticUserDisabledActivity disabled = Assert.IsType<AgenticUserDisabledActivity>(activity);
                Assert.Equal(AgentLifecycleEventTypes.AgenticUserDisabled, disabled.Value?.EventType);
                Assert.Equal(7, disabled.Value?.Version);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserDeleted:
                AgenticUserDeletedActivity deleted = Assert.IsType<AgenticUserDeletedActivity>(activity);
                Assert.Equal("UserSoftDelete", deleted.Value?.DeletionReason);
                Assert.Equal(8, deleted.Value?.Version);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserUndeleted:
                AgenticUserUndeletedActivity undeleted = Assert.IsType<AgenticUserUndeletedActivity>(activity);
                Assert.Equal(AgentLifecycleEventTypes.AgenticUserUndeleted, undeleted.Value?.EventType);
                Assert.Equal(9, undeleted.Value?.Version);
                break;

            case AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated:
                AgenticUserWorkloadOnboardingUpdatedActivity workload = Assert.IsType<AgenticUserWorkloadOnboardingUpdatedActivity>(activity);
                Assert.Equal("Teams", workload.Value?.WorkloadName);
                Assert.Equal("succeeded", workload.Value?.WorkloadOnboardingState);
                break;
        }
    }

    private static Task DispatchAsync(TeamsBotApplication app, string valueType, TurnStateContainer? state = null)
    {
        EventActivity activity = Assert.IsAssignableFrom<EventActivity>(ParseLifecycleActivity(valueType));
        Context<TeamsActivity> context = new(app, activity);
        if (state is not null)
        {
            context.State = state;
        }

        return app.Router.DispatchAsync(context);
    }

    private static TeamsActivity ParseLifecycleActivity(string valueType)
        => ParseLifecycleActivity(valueType, ValueFor(valueType));

    private static TeamsActivity ParseLifecycleActivity(string valueType, string valueJson)
    {
        string json = $$"""
            {
              "recipient": {
                "agenticUserId": "{{AgenticUserId}}",
                "agenticAppId": "{{AppId}}",
                "agenticAppBlueprintId": "{{BlueprintId}}",
                "callbackUri": "https://example.test/api/messages",
                "tenantId": "{{TenantId}}",
                "role": "agenticUser",
                "id": "{{AgenticUserId}}"
              },
              "type": "event",
              "id": "activity-id",
              "timestamp": "2026-06-29T00:00:00Z",
              "serviceUrl": "https://smba.trafficmanager.net/amer/tenant/",
              "channelId": "agents",
              "from": { "id": "system", "name": "System", "tenantId": "{{TenantId}}" },
              "conversation": { "tenantId": "{{TenantId}}", "id": "conversation-id", "topic": null },
              "channelData": { "tenant": { "id": "{{TenantId}}" }, "productContext": null },
              "valueType": "{{valueType}}",
              "value": {{valueJson}},
              "name": "agentLifecycle"
            }
            """;

        return TeamsActivity.FromActivity(CoreActivity.FromJsonString(json));
    }

    private static string ValueFor(string valueType)
        => valueType switch
        {
            AgentLifecycleEventValueTypes.AgenticUserIdentityCreated => CommonValue(
                AgentLifecycleEventTypes.AgenticUserIdentityCreated,
                $$"""
                  "expirationDateTime": "0001-01-01T00:00:00+00:00",
                  "manager": {
                    "displayName": null,
                    "userId": "{{ManagerId}}",
                    "email": "manager@example.test"
                  }
                """),
            AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated => CommonValue(
                AgentLifecycleEventTypes.AgenticUserIdentityUpdated,
                """
                  "updatedProperty": {
                    "propertyName": "Mail",
                    "propertyValue": "newinstance4@teamssdk.onmicrosoft.com"
                  },
                  "version": 4
                """),
            AgentLifecycleEventValueTypes.AgenticUserManagerUpdated => CommonValue(
                AgentLifecycleEventTypes.AgenticUserManagerUpdated,
                $$"""
                  "manager": { "managerId": "{{ManagerId}}" },
                  "version": 6
                """),
            AgentLifecycleEventValueTypes.AgenticUserEnabled => CommonValue(
                AgentLifecycleEventTypes.AgenticUserEnabled,
                """
                  "version": 6
                """),
            AgentLifecycleEventValueTypes.AgenticUserDisabled => CommonValue(
                AgentLifecycleEventTypes.AgenticUserDisabled,
                """
                  "version": 7
                """),
            AgentLifecycleEventValueTypes.AgenticUserDeleted => CommonValue(
                AgentLifecycleEventTypes.AgenticUserDeleted,
                """
                  "deletionReason": "UserSoftDelete",
                  "version": 8
                """),
            AgentLifecycleEventValueTypes.AgenticUserUndeleted => CommonValue(
                AgentLifecycleEventTypes.AgenticUserUndeleted,
                """
                  "version": 9
                """),
            AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated => CommonValue(
                AgentLifecycleEventTypes.AgenticUserWorkloadOnboardingUpdated,
                """
                  "workloadName": "Teams",
                  "workloadOnboardingState": "succeeded"
                """),
            _ => CommonValue("unknownLifecycleEvent"),
        };

    private static string CommonValue(string eventType, string additionalJson = "")
    {
        string separator = string.IsNullOrWhiteSpace(additionalJson) ? string.Empty : "," + Environment.NewLine + additionalJson;
        return $$"""
            {
              "tenantId": "{{TenantId}}",
              "agenticUserId": "{{AgenticUserId}}",
              "agenticAppInstanceId": "{{AgenticAppInstanceId}}",
              "agentIdentityBlueprintId": "{{BlueprintId}}",
              "eventType": "{{eventType}}"{{separator}}
            }
            """;
    }

    private static int CountOccurrences(string value, string match)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(match, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += match.Length;
        }

        return count;
    }

    private static TurnStateContainer CreateState()
    {
        TurnState conversationState = new();
        conversationState.Set("test-key", "test-value");
        return new TurnStateContainer(conversationState, new TurnState());
    }

    private static TeamsBotApplication CreateApp()
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);

        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        return new TeamsBotApplication(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
    }
}
