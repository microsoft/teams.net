// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Moq;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    public class CompatConversationsTests
    {
        private const string TestServiceUrl = "https://smba.trafficmanager.net/amer/";
        private const string TestConversationId = "test-conversation-id";
        private const string TestActivityId = "test-activity-id";

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_SetsServiceUrlFromProperty_WhenActivityServiceUrlIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test message"
            };

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, string, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((act, _, _, _, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(TestServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_DoesNotOverrideServiceUrl_WhenActivityServiceUrlIsSet()
        {
            // Arrange
            const string activityServiceUrl = "https://custom.service.url/";
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test message",
                ServiceUrl = activityServiceUrl
            };

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, string, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((act, _, _, _, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(activityServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReplyToActivityWithHttpMessagesAsync_SetsServiceUrlFromProperty_WhenActivityServiceUrlIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test reply"
            };

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, string, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((act, _, _, _, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(TestServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            Assert.Equal(TestActivityId, capturedActivity.Properties["replyToId"]);
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReplyToActivityWithHttpMessagesAsync_DoesNotOverrideServiceUrl_WhenActivityServiceUrlIsSet()
        {
            // Arrange
            const string activityServiceUrl = "https://custom.service.url/";
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test reply",
                ServiceUrl = activityServiceUrl
            };

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, string, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((act, _, _, _, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(activityServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateActivityWithHttpMessagesAsync_SetsServiceUrlFromProperty_WhenActivityServiceUrlIsNull()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Updated message"
            };

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.UpdateActivityAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CoreActivity>(),
                    It.IsAny<bool>(),
                    It.IsAny<AgenticIdentity?>(),
                    It.IsAny<Dictionary<string, string>?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CoreActivity, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((_, _, act, _, _, _, _) => capturedActivity = act)
                .ReturnsAsync(new UpdateActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.UpdateActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(TestServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.UpdateActivityAsync(
                    TestConversationId,
                    TestActivityId,
                    It.IsAny<CoreActivity>(),
                    It.IsAny<bool>(),
                    It.IsAny<AgenticIdentity?>(),
                    It.IsAny<Dictionary<string, string>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateActivityWithHttpMessagesAsync_DoesNotOverrideServiceUrl_WhenActivityServiceUrlIsSet()
        {
            // Arrange
            const string activityServiceUrl = "https://custom.service.url/";
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Updated message",
                ServiceUrl = activityServiceUrl
            };

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.UpdateActivityAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CoreActivity>(),
                    It.IsAny<bool>(),
                    It.IsAny<AgenticIdentity?>(),
                    It.IsAny<Dictionary<string, string>?>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CoreActivity, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((_, _, act, _, _, _, _) => capturedActivity = act)
                .ReturnsAsync(new UpdateActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.UpdateActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(activityServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.UpdateActivityAsync(
                    TestConversationId,
                    TestActivityId,
                    It.IsAny<CoreActivity>(),
                    It.IsAny<bool>(),
                    It.IsAny<AgenticIdentity?>(),
                    It.IsAny<Dictionary<string, string>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_EnsuresConversationIdIsSet()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test message"
            };

            string? capturedConversationId = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, string, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((_, convId, _, _, _, _) => capturedConversationId = convId)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(capturedConversationId);
            Assert.Equal(TestConversationId, capturedConversationId);
        }

        [Fact]
        public async Task ReplyToActivityWithHttpMessagesAsync_SetsReplyToIdProperty()
        {
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test reply"
            };

            CoreActivity? capturedActivity = null;
            string? capturedConversationId = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, string, bool, AgenticIdentity?, Dictionary<string, string>?, CancellationToken>((act, convId, _, _, _, _) => { capturedActivity = act; capturedConversationId = convId; })
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, "parent-activity-id", activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.True(capturedActivity.Properties.ContainsKey("replyToId"));
            Assert.Equal("parent-activity-id", capturedActivity.Properties["replyToId"]);
            Assert.NotNull(capturedConversationId);
            Assert.Equal(TestConversationId, capturedConversationId);
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_WhenSendActivityReturnsNull_ReturnsStringEmptyForId()
        {
            // This test verifies the fix for the OAuth card null reference bug
            // When APX returns 202 Accepted with no body, SendActivityAsync returns null
            // We should return string.Empty for Id instead of null to maintain API contract

            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SendActivityResponse?)null); // Simulate 202 Accepted with no body

            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test message"
            };

            // Act
            var result = await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Body);
            Assert.Equal(string.Empty, result.Body.Id); // Should be string.Empty, not null
        }

        [Fact]
        public async Task ReplyToActivityWithHttpMessagesAsync_WhenSendActivityReturnsNull_ReturnsStringEmptyForId()
        {
            // This test verifies the fix for the OAuth card null reference bug in ReplyToActivity
            // When APX returns 202 Accepted with no body, SendActivityAsync returns null
            // We should return string.Empty for Id instead of null to maintain API contract

            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<AgenticIdentity?>(), It.IsAny<Dictionary<string, string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SendActivityResponse?)null); // Simulate 202 Accepted with no body

            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test reply"
            };

            // Act
            var result = await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Body);
            Assert.Equal(string.Empty, result.Body.Id); // Should be string.Empty, not null
        }

        private static Mock<ConversationClient> CreateMockConversationClient()
        {
            Mock<ConversationClient> mock = new(
                Mock.Of<HttpClient>(),
                null!);

            return mock;
        }
    }
}
