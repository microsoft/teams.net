// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class ContextStateTests
{
    [Fact]
    public void State_WhenConfigured_ReturnsContainer()
    {
        TeamsBotApplication app = CreateApp();
        TurnState convState = new();
        TurnState userState = new();

        Context<TeamsActivity> context = new(app, new TeamsActivity());
        context.State = new TurnStateContainer(convState, userState);

        Assert.Same(convState, context.State.ConversationState);
        Assert.Same(userState, context.State.UserState);
    }

    [Fact]
    public void State_WhenNull_ThrowsInvalidOperationException()
    {
        TeamsBotApplication app = CreateApp();

        Context<TeamsActivity> context = new(app, new TeamsActivity());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.State);
        Assert.Contains("UseState()", ex.Message);
    }

    [Fact]
    public void HasState_WhenNotSet_ReturnsFalse()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> context = new(app, new TeamsActivity());

        Assert.False(context.HasState);
    }

    [Fact]
    public void HasState_WhenSet_ReturnsTrue()
    {
        TeamsBotApplication app = CreateApp();
        Context<TeamsActivity> context = new(app, new TeamsActivity());
        context.State = new TurnStateContainer(new TurnState(), null);

        Assert.True(context.HasState);
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
