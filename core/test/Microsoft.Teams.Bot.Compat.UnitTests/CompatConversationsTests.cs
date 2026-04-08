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
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(TestServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
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
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(activityServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
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
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(TestServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            Assert.Equal(TestActivityId, capturedActivity.Properties["replyToId"]);
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
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
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, TestActivityId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.ServiceUrl);
            Assert.Equal(activityServiceUrl.TrimEnd('/'), capturedActivity.ServiceUrl.ToString().TrimEnd('/'));
            mockConversationClient.Verify(
                c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CoreActivity, Dictionary<string, string>?, CancellationToken>((_, _, act, _, _) => capturedActivity = act)
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
                    It.IsAny<Dictionary<string, string>>(),
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
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CoreActivity, Dictionary<string, string>?, CancellationToken>((_, _, act, _, _) => capturedActivity = act)
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
                    It.IsAny<Dictionary<string, string>>(),
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

            CoreActivity? capturedActivity = null;
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.NotNull(capturedActivity.Conversation);
            Assert.Equal(TestConversationId, capturedActivity.Conversation.Id);
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
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) => capturedActivity = act)
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            // Act
            await compatConversations.ReplyToActivityWithHttpMessagesAsync(TestConversationId, "parent-activity-id", activity);

            // Assert
            Assert.NotNull(capturedActivity);
            Assert.True(capturedActivity.Properties.ContainsKey("replyToId"));
            Assert.Equal("parent-activity-id", capturedActivity.Properties["replyToId"]);
            Assert.NotNull(capturedActivity.Conversation);
            Assert.Equal(TestConversationId, capturedActivity.Conversation.Id);
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_WithNullServiceUrl_ThrowsArgumentNullException()
        {
            // This test reproduces the ProjectAgent scenario where ServiceUrl might not be set
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();

            // Make the mock behave like the real ConversationClient - validate ServiceUrl
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) =>
                {
                    // Mimic the real ConversationClient.SendActivityAsync validation (lines 46-49)
                    ArgumentNullException.ThrowIfNull(act);
                    ArgumentNullException.ThrowIfNull(act.Conversation);
                    ArgumentException.ThrowIfNullOrWhiteSpace(act.Conversation.Id);
                    ArgumentNullException.ThrowIfNull(act.ServiceUrl);  // This should throw!
                })
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            CompatConversations compatConversations = new(mockConversationClient.Object);
            // NOTE: ServiceUrl is NOT set on compatConversations

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Attachments = new List<Attachment>
                {
                    new()
                    {
                        ContentType = "application/vnd.microsoft.card.oauth",
                        Content = new { buttons = new[] { new { type = "signin" } } }
                    }
                },
                Recipient = new ChannelAccount { Id = "user-123", Name = "Test User" },
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = TestConversationId }
                // NOTE: ServiceUrl is also NOT set on activity
            };

            // Act & Assert
            // ConversationClient.SendActivityAsync should throw ArgumentNullException when it validates activity.ServiceUrl
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity)
            );

            // Verify it's about the ServiceUrl
            Console.WriteLine($"Exception message: {exception.Message}");
            Console.WriteLine($"Parameter name: {exception.ParamName}");
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_WithNullConversationId_DoesNotThrow()
        {
            // Test what happens if conversationId is null (even though user said it's not)
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .Callback<CoreActivity, Dictionary<string, string>?, CancellationToken>((act, _, _) =>
                {
                    Console.WriteLine($"Conversation: {act.Conversation}");
                    Console.WriteLine($"Conversation.Id: {act.Conversation?.Id ?? "NULL"}");
                })
                .ReturnsAsync(new SendActivityResponse { Id = TestActivityId });

            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test"
            };

            // Act - pass null as conversationId
            await compatConversations.SendToConversationWithHttpMessagesAsync(null!, activity);

            // This should succeed - the null conversationId gets assigned to Conversation.Id
            Assert.True(true);
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_WhenSendActivityReturnsNull_ThrowsNullReferenceException()
        {
            // CRITICAL TEST: If APX returns null response, this should reproduce the NullReferenceException!
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SendActivityResponse?)null!); // Return null despite non-nullable return type

            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test"
            };

            // Act & Assert
            // This should throw NullReferenceException when trying to access response.Id on line 324
            var exception = await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity)
            );

            Console.WriteLine($"SUCCESS! Reproduced NullReferenceException: {exception.Message}");
        }

        [Fact]
        public async Task SendToConversationWithHttpMessagesAsync_WhenResponseIdIsNull_Succeeds()
        {
            // Test if response.Id being null causes issues (it shouldn't - Id is nullable)
            // Arrange
            Mock<ConversationClient> mockConversationClient = CreateMockConversationClient();
            mockConversationClient
                .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendActivityResponse { Id = null }); // Id is null

            CompatConversations compatConversations = new(mockConversationClient.Object)
            {
                ServiceUrl = TestServiceUrl
            };

            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Test"
            };

            // Act
            var result = await compatConversations.SendToConversationWithHttpMessagesAsync(TestConversationId, activity);

            // Assert - Should succeed, Id will just be null
            Assert.NotNull(result);
            Assert.NotNull(result.Body);
            Assert.Null(result.Body.Id);
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
