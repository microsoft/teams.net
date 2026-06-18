// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.BotBuilder.UnitTests
{
    public class TeamsBotFrameworkHttpAdapterTests
    {
        [Fact]
        public async Task ContinueConversationAsync_WhenCastToBotAdapter_BuildsTurnContextWithUnderlyingClients()
        {
            // Arrange
            TeamsBotFrameworkHttpAdapter compatAdapter = CreateCompatAdapter();

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

        [Fact]
        public async Task ProcessAsync_OnTurnError_ReceivesTurnContextWithTurnState()
        {
            // Arrange
            TeamsBotFrameworkHttpAdapter adapter = CreateCompatAdapter();

            ITurnContext? capturedTurnContext = null;
            Microsoft.Bot.Connector.Authentication.UserTokenClient? capturedUserTokenClient = null;
            Microsoft.Bot.Connector.IConnectorClient? capturedConnectorClient = null;
            string? capturedCustomTurnState = null;

            adapter.OnTurnError = (turnContext, exception) =>
            {
                capturedTurnContext = turnContext;
                capturedUserTokenClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.Authentication.UserTokenClient>();
                capturedConnectorClient = turnContext.TurnState.Get<Microsoft.Bot.Connector.IConnectorClient>();
                capturedCustomTurnState = turnContext.TurnState.Get<string>("customTurnStateKey");
                return Task.CompletedTask;
            };

            Mock<IBot> mockBot = new();
            mockBot
                .Setup(b => b.OnTurnAsync(It.IsAny<ITurnContext>(), It.IsAny<CancellationToken>()))
                .Returns<ITurnContext, CancellationToken>((tc, _) =>
                {
                    tc.TurnState.Add("customTurnStateKey", "customTurnStateValue");
                    throw new InvalidOperationException("Test exception");
                });

            CoreActivity activity = new()
            {
                Type = ActivityType.Message,
                Id = "act123",
                ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/"),
                Conversation = new Conversation("conv123"),
                From = new Teams.Core.Schema.ChannelAccount { Id = "user123" }
            };

            DefaultHttpContext httpContext = new();
            byte[] bodyBytes = Encoding.UTF8.GetBytes(activity.ToJson());
            httpContext.Request.Body = new MemoryStream(bodyBytes);
            httpContext.Request.ContentType = "application/json";

            // Act
            await adapter.ProcessAsync(httpContext.Request, httpContext.Response, mockBot.Object, CancellationToken.None);

            // Assert
            Assert.NotNull(capturedTurnContext);

            Assert.NotNull(capturedUserTokenClient);
            Assert.Equal("CompatUserTokenClient", capturedUserTokenClient.GetType().Name);

            Assert.NotNull(capturedConnectorClient);
            Assert.Equal("CompatConnectorClient", capturedConnectorClient.GetType().Name);

            Assert.Equal("customTurnStateValue", capturedCustomTurnState);
        }

        private static TeamsBotFrameworkHttpAdapter CreateCompatAdapter()
        {
            HttpClient httpClient = new();
            ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);

            Mock<IConfiguration> mockConfig = new();
            mockConfig.Setup(c => c["UserTokenApiEndpoint"]).Returns("https://token.botframework.com");

            UserTokenClient userTokenClient = new(httpClient, mockConfig.Object, NullLogger<UserTokenClient>.Instance);

            BotApplication botApplication = new(
                conversationClient,
                userTokenClient,
                NullLogger<BotApplication>.Instance);

            TeamsBotFrameworkHttpAdapter compatAdapter = new(
                botApplication,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<TeamsBotFrameworkHttpAdapter>.Instance);

            return compatAdapter;
        }
    }
}
