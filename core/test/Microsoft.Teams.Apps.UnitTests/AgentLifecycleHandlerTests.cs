// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
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

    public static TheoryData<AgentLifecycleEventValueType, Type> VariantTypes => new()
    {
        { AgentLifecycleEventValueTypes.AgenticUserIdentityCreated, typeof(AgentLifecycleEventActivity<AgenticUserIdentityCreatedValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated, typeof(AgentLifecycleEventActivity<AgenticUserIdentityUpdatedValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserManagerUpdated, typeof(AgentLifecycleEventActivity<AgenticUserManagerUpdatedValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserEnabled, typeof(AgentLifecycleEventActivity<AgenticUserEnabledValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserDisabled, typeof(AgentLifecycleEventActivity<AgenticUserDisabledValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserDeleted, typeof(AgentLifecycleEventActivity<AgenticUserDeletedValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserUndeleted, typeof(AgentLifecycleEventActivity<AgenticUserUndeletedValue>) },
        { AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated, typeof(AgentLifecycleEventActivity<AgenticUserWorkloadOnboardingUpdatedValue>) },
    };

    [Theory]
    [MemberData(nameof(VariantTypes))]
    public void FromActivity_ParsesAgentLifecycleEventEnvelope(AgentLifecycleEventValueType valueType, Type _)
    {
        TeamsActivity activity = ParseLifecycleActivity(valueType);

        EventActivity eventActivity = Assert.IsType<EventActivity>(activity);
        Assert.Equal(TeamsActivityTypes.Event, eventActivity.Type);
        Assert.Equal(EventNames.AgentLifecycle, eventActivity.Name);
        Assert.Equal(valueType, eventActivity.Properties.Get<AgentLifecycleEventValueType>("valueType"));
        Assert.NotNull(eventActivity.Value);
    }

    [Fact]
    public void FromActivity_KeepsUnknownAgentLifecycleValueTypeAsBaseLifecycleActivity()
    {
        TeamsActivity activity = ParseLifecycleActivity(new AgentLifecycleEventValueType("UnknownLifecycleValueType"), CommonValue(new AgentLifecycleEventType("unknownLifecycleEvent")));

        EventActivity eventActivity = Assert.IsType<EventActivity>(activity);
        Assert.Equal(EventNames.AgentLifecycle, eventActivity.Name);
        Assert.Equal("UnknownLifecycleValueType", eventActivity.Properties.Get<string>("valueType"));
        Assert.NotNull(eventActivity.Value);
    }

    [Fact]
    public async Task OnEvent_StillReceivesAgentLifecycleEvents()
    {
        TeamsBotApplication app = CreateApp();
        bool called = false;

        app.OnEvent((ctx, _) =>
        {
            called = true;
            Assert.IsType<EventActivity>(ctx.Activity);
            Assert.Equal(EventNames.AgentLifecycle, ctx.Activity.Name);
            Assert.Equal(AgentLifecycleEventValueTypes.AgenticUserEnabled, ctx.Activity.Properties.Get<AgentLifecycleEventValueType>("valueType"));
            return Task.CompletedTask;
        });

        await DispatchAsync(app, AgentLifecycleEventValueTypes.AgenticUserEnabled);

        Assert.True(called);
    }

    [Theory]
    [MemberData(nameof(VariantTypes))]
    public async Task VariantHandlers_DispatchTypedContextAndPropagateState(AgentLifecycleEventValueType valueType, Type expectedType)
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer state = CreateState();
        int lifecycleHandlerCalls = 0;
        int variantHandlerCalls = 0;

        app.OnAgentLifecycle((ctx, _) =>
        {
            lifecycleHandlerCalls++;
            Assert.Same(state, ctx.State);
            Assert.Equal(typeof(AgentLifecycleEventActivity), ctx.Activity.GetType());
            Assert.Equal(valueType, ctx.Activity.ValueType!);
            Assert.False(ctx.Activity.Properties.ContainsKey("valueType"));
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
        AgentLifecycleEventValueType valueType,
        Type expectedType,
        TurnStateContainer state,
        Action onCalled)
    {
        if (valueType == AgentLifecycleEventValueTypes.AgenticUserIdentityCreated)
        {
            app.OnAgenticUserIdentityCreated((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal(ManagerId, ctx.Activity.Value?.Manager?.UserId);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated)
        {
            app.OnAgenticUserIdentityUpdated((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal("Mail", ctx.Activity.Value?.UpdatedProperty.PropertyName);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserManagerUpdated)
        {
            app.OnAgenticUserManagerUpdated((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal(ManagerId, ctx.Activity.Value?.Manager?.ManagerId);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserEnabled)
        {
            app.OnAgenticUserEnabled((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal(6, ctx.Activity.Value?.Version);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserDisabled)
        {
            app.OnAgenticUserDisabled((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal(7, ctx.Activity.Value?.Version);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserDeleted)
        {
            app.OnAgenticUserDeleted((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal("UserSoftDelete", ctx.Activity.Value?.DeletionReason);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserUndeleted)
        {
            app.OnAgenticUserUndeleted((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal(9, ctx.Activity.Value?.Version);
                return Task.CompletedTask;
            });
        }
        else if (valueType == AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated)
        {
            app.OnAgenticUserWorkloadOnboardingUpdated((ctx, _) =>
            {
                AssertVariantContext(ctx, expectedType, valueType, state, onCalled);
                Assert.Equal("Teams", ctx.Activity.Value?.WorkloadName);
                Assert.Equal("succeeded", ctx.Activity.Value?.WorkloadOnboardingState);
                return Task.CompletedTask;
            });
        }
    }

    private static void AssertVariantContext<TActivity>(
        Context<TActivity> context,
        Type expectedType,
        AgentLifecycleEventValueType valueType,
        TurnStateContainer state,
        Action onCalled) where TActivity : AgentLifecycleEventActivity
    {
        onCalled();
        Assert.Same(state, context.State);
        Assert.Equal(expectedType, context.Activity.GetType());
        Assert.Equal(EventNames.AgentLifecycle, context.Activity.Name);
        Assert.Equal(valueType, context.Activity.ValueType);
        Assert.False(context.Activity.Properties.ContainsKey("valueType"));
    }

    private static Task DispatchAsync(TeamsBotApplication app, AgentLifecycleEventValueType valueType, TurnStateContainer? state = null)
    {
        EventActivity activity = Assert.IsAssignableFrom<EventActivity>(ParseLifecycleActivity(valueType));
        Context<TeamsActivity> context = new(app, activity);
        if (state is not null)
        {
            context.State = state;
        }

        return app.Router.DispatchAsync(context);
    }

    private static TeamsActivity ParseLifecycleActivity(AgentLifecycleEventValueType valueType)
        => ParseLifecycleActivity(valueType, ValueFor(valueType));

    private static TeamsActivity ParseLifecycleActivity(AgentLifecycleEventValueType valueType, string valueJson)
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

    private static string ValueFor(AgentLifecycleEventValueType valueType)
    {
        if (valueType == AgentLifecycleEventValueTypes.AgenticUserIdentityCreated)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserIdentityCreated,
                $$"""
                  "expirationDateTime": "0001-01-01T00:00:00+00:00",
                  "manager": {
                    "displayName": null,
                    "userId": "{{ManagerId}}",
                    "email": "manager@example.test"
                  }
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserIdentityUpdated,
                """
                  "updatedProperty": {
                    "propertyName": "Mail",
                    "propertyValue": "newinstance4@teamssdk.onmicrosoft.com"
                  },
                  "version": 4
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserManagerUpdated)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserManagerUpdated,
                $$"""
                  "manager": { "managerId": "{{ManagerId}}" },
                  "version": 6
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserEnabled)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserEnabled,
                """
                  "version": 6
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserDisabled)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserDisabled,
                """
                  "version": 7
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserDeleted)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserDeleted,
                """
                  "deletionReason": "UserSoftDelete",
                  "version": 8
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserUndeleted)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserUndeleted,
                """
                  "version": 9
                """);
        }

        if (valueType == AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated)
        {
            return CommonValue(
                AgentLifecycleEventTypes.AgenticUserWorkloadOnboardingUpdated,
                """
                  "workloadName": "Teams",
                  "workloadOnboardingState": "succeeded"
                """);
        }

        return CommonValue(new AgentLifecycleEventType("unknownLifecycleEvent"));
    }

    private static string CommonValue(AgentLifecycleEventType eventType, string additionalJson = "")
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
