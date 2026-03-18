// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Moq;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    public class CompatBotAdapterTests
    {
        [Fact]
        public async Task DeleteActivityAsync_UsesConversationReferenceValues()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            ConversationReference reference = new()
            {
                ActivityId = "activity-123",
                ServiceUrl = "https://smba.trafficmanager.net/teams/",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-456" },
                ChannelId = "msteams"
            };

            ITurnContext turnContext = CreateMockTurnContext("https://different-service-url.com/");

            // Act
            await adapter.DeleteActivityAsync(turnContext, reference, CancellationToken.None);

            // Assert
            mockConversationClient.Verify(
                c => c.DeleteActivityAsync(
                    "conversation-456",
                    "activity-123",
                    It.Is<Uri>(u => u.ToString().TrimEnd('/') == "https://smba.trafficmanager.net/teams"),
                    It.IsAny<AgenticIdentity>(),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteActivityAsync_FallsBackToTurnContextServiceUrl_WhenReferenceServiceUrlIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            ConversationReference reference = new()
            {
                ActivityId = "activity-123",
                ServiceUrl = null, // Not set in reference
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-456" },
                ChannelId = "msteams"
            };

            ITurnContext turnContext = CreateMockTurnContext("https://fallback-service-url.com/");

            // Act
            await adapter.DeleteActivityAsync(turnContext, reference, CancellationToken.None);

            // Assert
            mockConversationClient.Verify(
                c => c.DeleteActivityAsync(
                    "conversation-456",
                    "activity-123",
                    It.Is<Uri>(u => u.ToString().TrimEnd('/') == "https://fallback-service-url.com"),
                    It.IsAny<AgenticIdentity>(),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteActivityAsync_ThrowsArgumentException_WhenConversationIdIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            ConversationReference reference = new()
            {
                ActivityId = "activity-123",
                ServiceUrl = "https://smba.trafficmanager.net/teams/",
                Conversation = null, // No conversation
                ChannelId = "msteams"
            };

            ITurnContext turnContext = CreateMockTurnContext("https://service-url.com/");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await adapter.DeleteActivityAsync(turnContext, reference, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteActivityAsync_ThrowsArgumentException_WhenActivityIdIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            ConversationReference reference = new()
            {
                ActivityId = null, // No activity ID
                ServiceUrl = "https://smba.trafficmanager.net/teams/",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-456" },
                ChannelId = "msteams"
            };

            ITurnContext turnContext = CreateMockTurnContext("https://service-url.com/");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await adapter.DeleteActivityAsync(turnContext, reference, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteActivityAsync_ThrowsArgumentException_WhenServiceUrlIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            ConversationReference reference = new()
            {
                ActivityId = "activity-123",
                ServiceUrl = null, // No service URL in reference
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-456" },
                ChannelId = "msteams"
            };

            ITurnContext turnContext = CreateMockTurnContext(null); // No service URL in turn context either

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await adapter.DeleteActivityAsync(turnContext, reference, CancellationToken.None));
        }

        [Fact]
        public async Task SendActivitiesAsync_SetsServiceUrlFromTurnContext()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient.Setup(c => c.SendActivityAsync(
                    It.IsAny<CoreActivity>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendActivityResponse { Id = "sent-123" });

            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Hello",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-123" },
                ServiceUrl = null // ServiceUrl not set on activity
            };

            ITurnContext turnContext = CreateMockTurnContext("https://turn-context-service-url.com/");

            // Act
            ResourceResponse[] responses = await adapter.SendActivitiesAsync(turnContext, [activity], CancellationToken.None);

            // Assert
            Assert.Single(responses);
            Assert.Equal("sent-123", responses[0].Id);

            mockConversationClient.Verify(
                c => c.SendActivityAsync(
                    It.Is<CoreActivity>(a => a.ServiceUrl != null && a.ServiceUrl.ToString().TrimEnd('/') == "https://turn-context-service-url.com"),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateActivityAsync_SetsServiceUrlFromTurnContext()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient.Setup(c => c.UpdateActivityAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CoreActivity>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateActivityResponse { Id = "updated-123" });

            CompatBotAdapter adapter = CreateCompatBotAdapter(mockConversationClient.Object);

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Id = "activity-123",
                Text = "Updated message",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-123" },
                ServiceUrl = null // ServiceUrl not set on activity
            };

            ITurnContext turnContext = CreateMockTurnContext("https://turn-context-service-url.com/");

            // Act
            ResourceResponse response = await adapter.UpdateActivityAsync(turnContext, activity, CancellationToken.None);

            // Assert
            Assert.Equal("updated-123", response.Id);

            mockConversationClient.Verify(
                c => c.UpdateActivityAsync(
                    "conversation-123",
                    "activity-123",
                    It.Is<CoreActivity>(a => a.ServiceUrl != null && a.ServiceUrl.ToString().TrimEnd('/') == "https://turn-context-service-url.com"),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static Mock<ConversationClient> CreateMockConversationClient()
        {
            Mock<ConversationClient> mock = new(
                new HttpClient(),
                NullLogger<ConversationClient>.Instance);

            mock.Setup(c => c.DeleteActivityAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Uri>(),
                    It.IsAny<AgenticIdentity>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mock.Setup(c => c.SendActivityAsync(
                    It.IsAny<CoreActivity>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendActivityResponse { Id = "default-sent-id" });

            return mock;
        }

        private static Mock<TeamsBotApplication> CreateMockTeamsBotApplication()
        {
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            Mock<UserTokenClient> mockUserTokenClient = new(
                new HttpClient(),
                Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
                NullLogger<UserTokenClient>.Instance);
            Mock<TeamsApiClient> mockTeamsApiClient = new(
                new HttpClient(),
                NullLogger<TeamsApiClient>.Instance);

            Mock<TeamsBotApplication> mock = new(
                mockConversationClient.Object,
                mockUserTokenClient.Object,
                mockTeamsApiClient.Object,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<TeamsBotApplication>.Instance);

            return mock;
        }

        private static CompatBotAdapter CreateCompatBotAdapter(ConversationClient conversationClient)
        {
            Mock<UserTokenClient> mockUserTokenClient = new(
                new HttpClient(),
                Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(),
                NullLogger<UserTokenClient>.Instance);
            Mock<TeamsApiClient> mockTeamsApiClient = new(
                new HttpClient(),
                NullLogger<TeamsApiClient>.Instance);

            TeamsBotApplication teamsBotApplication = new(
                conversationClient,
                mockUserTokenClient.Object,
                mockTeamsApiClient.Object,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<TeamsBotApplication>.Instance);

            return new CompatBotAdapter(
                teamsBotApplication,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<CompatBotAdapter>.Instance);
        }

        private static CompatBotAdapter CreateCompatBotAdapter(TeamsBotApplication teamsBotApplication)
        {
            return new CompatBotAdapter(
                teamsBotApplication,
                Mock.Of<IHttpContextAccessor>(),
                NullLogger<CompatBotAdapter>.Instance);
        }

        private static ITurnContext CreateMockTurnContext(string? serviceUrl)
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Id = "turn-activity-123",
                ServiceUrl = serviceUrl,
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "turn-conversation-123" },
                From = new ChannelAccount { Id = "user-123" },
                Recipient = new ChannelAccount { Id = "bot-123" },
                ChannelId = "msteams"
            };

            Mock<ITurnContext> mockTurnContext = new();
            mockTurnContext.Setup(t => t.Activity).Returns(activity);

            return mockTurnContext.Object;
        }
    }
}
