// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Handlers.MessageExtension;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Verifies that all handler extension methods propagate State from the
/// original context to the typed context passed to the user's handler.
/// </summary>
public class HandlerStatePropagationTests
{
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
    public async Task OnMessageFetchTask_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnMessageFetchTask((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<Handlers.TaskModules.TaskModuleResponse>(200));
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
    public async Task OnTaskSubmit_PropagatesState()
    {
        TeamsBotApplication app = CreateApp();
        TurnStateContainer? captured = null;

        app.OnTaskSubmit((ctx, _) =>
        {
            captured = ctx.State;
            return Task.FromResult(new InvokeResponse<Handlers.TaskModules.TaskModuleResponse>(200));
        });

        Context<TeamsActivity> context = CreateInvokeContext(app, InvokeNames.TaskSubmit);
        TurnStateContainer state = CreateState();
        context.State = state;

        await app.Router.DispatchWithReturnAsync(context);

        Assert.NotNull(captured);
        Assert.Same(state, captured);
    }

    private static TurnStateContainer CreateState()
    {
        TurnState convState = new();
        convState.Set("test-key", "test-value");
        return new TurnStateContainer(convState, new TurnState());
    }

    private static Context<TeamsActivity> CreateInvokeContext(TeamsBotApplication app, string invokeName)
    {
        InvokeActivity activity = new() { Type = TeamsActivityType.Invoke, Name = invokeName };
        return new Context<TeamsActivity>(app, activity);
    }

    private static Context<TeamsActivity> CreateEventContext(TeamsBotApplication app, string eventName)
    {
        EventActivity activity = new() { Type = TeamsActivityType.Event, Name = eventName };
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
