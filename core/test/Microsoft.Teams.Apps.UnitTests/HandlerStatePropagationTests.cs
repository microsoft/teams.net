// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.MessageExtension;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Apps.TaskModules;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Verifies that all handler extension methods propagate State from the
/// original context to the typed context passed to the user's handler,
/// and that handlers work without state configured.
/// </summary>
public class HandlerStatePropagationTests
{
    // ===== Invoke handlers with state =====

    [Fact]
    public async Task OnAdaptiveCardAction_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnAdaptiveCardAction((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.AdaptiveCardAction);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnFileConsent_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnFileConsent((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<AdaptiveCardResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.FileConsent);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnQuery_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnQuery((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionQuery);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnSubmitAction_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnSubmitAction((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionActionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionSubmitAction);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnQueryLink_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnQueryLink((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionQueryLink);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnAnonQueryLink_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnAnonQueryLink((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionAnonQueryLink);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnFetchTask_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnFetchTask((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionActionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionFetchTask);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnSelectItem_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnSelectItem((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionSelectItem);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnQuerySettingUrl_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnQuerySettingUrl((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<MessageExtensionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionQuerySettingUrl);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnMessageFetchTask_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMessageFetchTask((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<TaskModuleResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageFetchTask);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnMessageSubmitAction_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMessageSubmitAction((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageSubmitAction);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnMessageSubmitFeedback_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMessageSubmitFeedback((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse(200));
        });

        InvokeActivity activity = new()
        {
            Type = TeamsActivityTypes.Invoke,
            Name = InvokeNames.MessageSubmitAction,
            Value = new JsonObject
            {
                ["actionName"] = "feedback",
                ["actionValue"] = new JsonObject()
            }
        };

        Context<TeamsActivity> context = new(app, activity);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnTaskFetch_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnTaskFetch((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<TaskModuleResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.TaskFetch);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnTaskSubmit_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnTaskSubmit((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<TaskModuleResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.TaskSubmit);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    // ===== Event handlers with state =====

    [Fact]
    public async Task OnMeetingStart_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMeetingStart((ctx, _) =>
        {
            captured = ctx.State;
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateEventContext(app, EventNames.MeetingStart);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnMeetingEnd_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMeetingEnd((ctx, _) =>
        {
            captured = ctx.State;
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateEventContext(app, EventNames.MeetingEnd);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnMeetingJoin_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMeetingJoin((ctx, _) =>
        {
            captured = ctx.State;
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateEventContext(app, EventNames.MeetingParticipantJoin);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    [Fact]
    public async Task OnMeetingLeave_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMeetingLeave((ctx, _) =>
        {
            captured = ctx.State;
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateEventContext(app, EventNames.MeetingParticipantLeave);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    // ===== Without state (HasState guard) =====

    [Fact]
    public async Task OnAdaptiveCardAction_WorksWithoutState()
    {
        TeamsBotApplication app = CreateApp();
        bool handlerCalled = false;

        app.OnAdaptiveCardAction((ctx, _) =>
        {
            handlerCalled = true;
            Assert.False(ctx.HasState);
            return Task.FromResult(new InvokeResponse(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.AdaptiveCardAction);
        // No state set on context

        await app.Router.DispatchWithReturnAsync(context);

        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task OnMeetingStart_WorksWithoutState()
    {
        TeamsBotApplication app = CreateApp();
        bool handlerCalled = false;

        app.OnMeetingStart((ctx, _) =>
        {
            handlerCalled = true;
            Assert.False(ctx.HasState);
            return Task.CompletedTask;
        });

        Context<TeamsActivity> context = CreateEventContext(app, EventNames.MeetingStart);
        // No state set on context

        await app.Router.DispatchAsync(context);

        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task OnQuery_WorksWithoutState()
    {
        TeamsBotApplication app = CreateApp();
        bool handlerCalled = false;

        app.OnQuery((ctx, _) =>
        {
            handlerCalled = true;
            Assert.False(ctx.HasState);
            return Task.FromResult(new InvokeResponse<MessageExtensionResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.MessageExtensionQuery);
        // No state set on context

        await app.Router.DispatchWithReturnAsync(context);

        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task OnTaskFetch_WorksWithoutState()
    {
        TeamsBotApplication app = CreateApp();
        bool handlerCalled = false;

        app.OnTaskFetch((ctx, _) =>
        {
            handlerCalled = true;
            Assert.False(ctx.HasState);
            return Task.FromResult(new InvokeResponse<TaskModuleResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.TaskFetch);
        // No state set on context

        await app.Router.DispatchWithReturnAsync(context);

        Assert.True(handlerCalled);
    }

    // ===== Helpers =====

    private static TurnStateContainer CreateState()
    {
        TurnState convState = new();
        convState.Set("test-key", "test-value");
        return new TurnStateContainer(convState, new TurnState());
    }

    private static Context<TeamsActivity> CreateInvokeContext(TeamsBotApplication app, InvokeName invokeName)
    {
        InvokeActivity activity = new(invokeName);
        return new Context<TeamsActivity>(app, activity);
    }

    private static Context<TeamsActivity> CreateEventContext(TeamsBotApplication app, EventName eventName)
    {
        EventActivity activity = new() { Type = TeamsActivityTypes.Event, Name = eventName };
        return new Context<TeamsActivity>(app, activity);
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
