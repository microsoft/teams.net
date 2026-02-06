// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Routing;
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
            var (compatAdapter, teamsApiClient) = CreateCompatAdapter();

            // Cast to BotAdapter to ensure we're using the base class method
            BotAdapter botAdapter = compatAdapter;

            var conversationReference = new ConversationReference
            {
                ServiceUrl = "https://smba.trafficmanager.net/teams",
                ChannelId = "msteams",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "test-conversation-id" }
            };

            bool callbackInvoked = false;
            Microsoft.Bot.Connector.Authentication.UserTokenClient? capturedUserTokenClient = null;
            Microsoft.Bot.Connector.IConnectorClient? capturedConnectorClient = null;
            Microsoft.Teams.Bot.Apps.TeamsApiClient? capturedTeamsApiClient = null;

            BotCallbackHandler callback = async (turnContext, cancellationToken) =>
            {
                callbackInvoked = true;
                capturedUserTokenClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.Authentication.UserTokenClient>();
                capturedConnectorClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.IConnectorClient>();
                capturedTeamsApiClient = turnContext.TurnState.Get<Microsoft.Teams.Bot.Apps.TeamsApiClient>();
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

            // Verify TeamsApiClient is the same instance we set up
            Assert.NotNull(capturedTeamsApiClient);
            Assert.Same(teamsApiClient, capturedTeamsApiClient);
        }

        private static (CompatAdapter, TeamsApiClient) CreateCompatAdapter()
        {
            var httpClient = new HttpClient();
            var conversationClient = new ConversationClient(httpClient, NullLogger<ConversationClient>.Instance);

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["UserTokenApiEndpoint"]).Returns("https://token.botframework.com");

            var userTokenClient = new UserTokenClient(httpClient, mockConfig.Object, NullLogger<UserTokenClient>.Instance);
            var teamsApiClient = new TeamsApiClient(httpClient, NullLogger<TeamsApiClient>.Instance);
            var router = new Router(NullLogger<Router>.Instance);

            var teamsBotApplication = new TeamsBotApplication(
                conversationClient,
                userTokenClient,
                teamsApiClient,
                mockConfig.Object,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<BotApplication>.Instance,
                router);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(TeamsBotApplication)))
                .Returns(teamsBotApplication);

            var compatAdapter = new CompatAdapter(mockServiceProvider.Object);

            return (compatAdapter, teamsApiClient);
        }
    }
}
