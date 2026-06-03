// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.State;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class ContextStateTests
{
    // ==================== State accessor ====================

    [Fact]
    public void State_WhenTurnStateIsSet_ReturnsTurnState()
    {
        // Arrange
        TeamsBotApplication app = CreateApp();
        Mock<ITurnState> mockState = new();
        app.TurnState = mockState.Object;

        Context<TeamsActivity> context = new(app, new TeamsActivity());

        // Act
        ITurnState result = context.State;

        // Assert
        Assert.Same(mockState.Object, result);
    }

    [Fact]
    public void State_WhenTurnStateIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        TeamsBotApplication app = CreateApp();
        // TurnState is null by default

        Context<TeamsActivity> context = new(app, new TeamsActivity());

        // Act & Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => context.State);
        Assert.Contains("AddBotApplicationState()", ex.Message);
    }

    // ==================== Helpers ====================

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
