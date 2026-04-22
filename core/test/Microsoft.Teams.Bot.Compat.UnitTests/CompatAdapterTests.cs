// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Moq;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    public class CompatAdapterTests
    {
        [Fact]
        public async Task ContinueConversationAsync_WhenCastToBotAdapter_BuildsTurnContextWithUnderlyingClients()
        {
            // Arrange
            (CompatAdapter? compatAdapter, TeamsApiClient? teamsApiClient) = CreateCompatAdapter();

            // Cast to BotAdapter to ensure we're using the base class method
            BotAdapter botAdapter = compatAdapter;

            ConversationReference conversationReference = new()
            {
                ServiceUrl = "https://smba.trafficmanager.net/teams",
                ChannelId = "msteams",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "test-conversation-id" }
            };

            bool callbackInvoked = false;
            Microsoft.Bot.Connector.Authentication.UserTokenClient? capturedUserTokenClient = null;
            Microsoft.Bot.Connector.IConnectorClient? capturedConnectorClient = null;

            BotCallbackHandler callback = async (turnContext, cancellationToken) =>
            {
                callbackInvoked = true;
                capturedUserTokenClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.Authentication.UserTokenClient>();
                capturedConnectorClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.IConnectorClient>();
                await Task.CompletedTask;
            };

            // Act
            await botAdapter.ContinueConversationAsync(
                "test-bot-id",
                conversationReference,
                callback,
                CancellationToken.None);

            // Assert
            Assert.True(callbackInvoked);

            // Verify UserTokenClient is CompatUserTokenClient (check by type name since it's internal)
            Assert.NotNull(capturedUserTokenClient);
            Assert.Equal("CompatUserTokenClient", capturedUserTokenClient.GetType().Name);
            Assert.IsAssignableFrom<Microsoft.Bot.Connector.Authentication.UserTokenClient>(capturedUserTokenClient);

            // Verify ConnectorClient is CompatConnectorClient (check by type name since it's internal)
            Assert.NotNull(capturedConnectorClient);
            Assert.Equal("CompatConnectorClient", capturedConnectorClient.GetType().Name);
            Assert.IsAssignableFrom<Microsoft.Bot.Connector.IConnectorClient>(capturedConnectorClient);
        }

        private static (CompatAdapter, TeamsApiClient) CreateCompatAdapter()
        {
            HttpClient httpClient = new();
            ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);

            Mock<IConfiguration> mockConfig = new();
            mockConfig.Setup(c => c["UserTokenApiEndpoint"]).Returns("https://token.botframework.com");

            UserTokenClient userTokenClient = new(httpClient, mockConfig.Object, NullLogger<UserTokenClient>.Instance);
            TeamsApiClient teamsApiClient = new(httpClient, NullLogger<TeamsApiClient>.Instance);

            TeamsBotApplication teamsBotApplication = new(
                conversationClient,
                userTokenClient,
                teamsApiClient,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<TeamsBotApplication>.Instance);

            CompatAdapter compatAdapter = new(
                teamsBotApplication,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<CompatAdapter>.Instance);

            return (compatAdapter, teamsApiClient);
        }
    }
}
