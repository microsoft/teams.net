// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Core.Schema;
using Newtonsoft.Json;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    public class CompatActivityTests
    {
        #region Core Properties Tests

        [Fact]
        public void FromCompatActivity_PreservesCoreProperties()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                ServiceUrl = "https://smba.trafficmanager.net/teams",
                ChannelId = "msteams",
                Id = "test-id-123",
                From = new ChannelAccount { Id = "user-123", Name = "Test User" },
                Recipient = new ChannelAccount { Id = "bot-456", Name = "Test Bot" },
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conv-789", Name = "Test Conversation" }
            };

            CoreActivity coreActivity = activity.FromCompatActivity();

            Assert.NotNull(coreActivity);
            Assert.Equal(activity.Type, coreActivity.Type);
            Assert.Equal(activity.ServiceUrl, coreActivity.ServiceUrl?.ToString());
            Assert.Equal(activity.ChannelId, coreActivity.ChannelId);
            Assert.Equal(activity.Id, coreActivity.Id);
            Assert.Equal(activity.From?.Id, coreActivity.From?.Id);
            Assert.Equal(activity.From?.Name, coreActivity.From?.Name);
            Assert.Equal(activity.Recipient?.Id, coreActivity.Recipient?.Id);
            Assert.Equal(activity.Conversation?.Id, coreActivity.Conversation?.Id);
        }

        [Fact]
        public void FromCompatActivity_PreservesTextAndMetadata()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Hello, this is a test message",
                TextFormat = "plain",
                Locale = "en-US",
                InputHint = "acceptingInput",
                ReplyToId = "reply-to-123"
            };

            CoreActivity coreActivity = activity.FromCompatActivity();

            Assert.NotNull(coreActivity);
            Assert.Equal(activity.Text, coreActivity.Properties["text"]?.ToString());
            Assert.Equal(activity.InputHint, coreActivity.Properties["inputHint"]?.ToString());
            Assert.Equal(activity.ReplyToId, coreActivity.ReplyToId);
            Assert.Equal(activity.Locale, coreActivity.Properties["locale"]?.ToString());
        }

        #endregion

        #region Attachments Tests

        [Fact]
        public void FromCompatActivity_PreservesAdaptiveCardAttachment()
        {
            string json = LoadTestData("AdaptiveCardActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;
            Assert.NotNull(botActivity);
            Assert.Single(botActivity.Attachments);

            CoreActivity coreActivity = botActivity.FromCompatActivity();

            Assert.NotNull(coreActivity);
            Assert.NotNull(coreActivity.Attachments);
            Assert.Single(coreActivity.Attachments);

            JsonNode? attachmentNode = coreActivity.Attachments[0];
            Assert.NotNull(attachmentNode);
            JsonObject attachmentObj = attachmentNode.AsObject();

            string? contentType = attachmentObj["contentType"]?.GetValue<string>();
            Assert.Equal("application/vnd.microsoft.card.adaptive", contentType);

            JsonNode? content = attachmentObj["content"];
            Assert.NotNull(content);
            AdaptiveCard card = AdaptiveCard.FromJson(content.ToJsonString()).Card;
            Assert.Equal(2, card.Body?.Count);
            AdaptiveTextBlock? firstTextBlock = card?.Body?[0] as AdaptiveTextBlock;
            Assert.NotNull(firstTextBlock);
            Assert.Equal("Mention a user by User Principle Name: Hello <at>Test User UPN</at>", firstTextBlock.Text);
        }

        [Fact]
        public void FromCompatActivity_PreservesMultipleAttachments()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Attachments = new List<Attachment>
                {
                    new() { ContentType = "text/plain", Content = "First attachment" },
                    new() { ContentType = "image/png", ContentUrl = "https://example.com/image.png" }
                }
            };

            CoreActivity coreActivity = activity.FromCompatActivity();

            Assert.NotNull(coreActivity.Attachments);
            Assert.Equal(2, coreActivity.Attachments?.Count);
            Assert.Equal("text/plain", coreActivity.Attachments?[0]?["contentType"]?.GetValue<string>());
            Assert.Equal("image/png", coreActivity.Attachments?[1]?["contentType"]?.GetValue<string>());
        }

        #endregion

        #region Entities Tests

        [Fact]
        public void FromCompatActivity_PreservesEntities()
        {
            string json = LoadTestData("AdaptiveCardActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromCompatActivity();

            Assert.NotNull(coreActivity.Entities);
            Assert.Single(coreActivity.Entities);

            JsonObject? entity = coreActivity.Entities[0]?.AsObject();
            Assert.NotNull(entity);
            Assert.Equal("https://schema.org/Message", entity["type"]?.GetValue<string>());
        }

        [Fact]
        public void FromCompatActivity_PreservesMultipleEntities()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromCompatActivity();

            Assert.NotNull(coreActivity.Entities);
            Assert.Equal(2, coreActivity.Entities?.Count);

            JsonObject? firstEntity = coreActivity.Entities?[0]?.AsObject();
            Assert.Equal("https://schema.org/Message", firstEntity?["type"]?.GetValue<string>());

            JsonObject? secondEntity = coreActivity.Entities?[1]?.AsObject();
            Assert.Equal("BotMessageMetadata", secondEntity?["type"]?.GetValue<string>());
        }

        #endregion

        #region SuggestedActions Tests

        [Fact]
        public void FromCompatActivity_PreservesSuggestedActions()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;
            Assert.NotNull(botActivity.SuggestedActions);
            Assert.Equal(3, botActivity.SuggestedActions.Actions.Count);

            CoreActivity coreActivity = botActivity.FromCompatActivity();

            Assert.True(coreActivity.Properties.ContainsKey("suggestedActions"));

            string coreActivityJson = coreActivity.ToJson();
            JsonNode coreActivityNode = JsonNode.Parse(coreActivityJson)!;

            JsonNode? suggestedActions = coreActivityNode["suggestedActions"];
            Assert.NotNull(suggestedActions);

            JsonArray? actions = suggestedActions["actions"]?.AsArray();
            Assert.NotNull(actions);
            Assert.Equal(3, actions.Count);
        }

        [Fact]
        public void FromCompatActivity_PreservesSuggestedActionDetails()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromCompatActivity();
            string coreActivityJson = coreActivity.ToJson();
            JsonNode coreActivityNode = JsonNode.Parse(coreActivityJson)!;

            JsonArray? actions = coreActivityNode["suggestedActions"]?["actions"]?.AsArray();
            Assert.NotNull(actions);

            // Verify Action.Odsl actions
            Assert.Equal("Action.Odsl", actions[0]?["type"]?.GetValue<string>());
            Assert.Equal("Add reviewers", actions[0]?["title"]?.GetValue<string>());
            Assert.NotNull(actions[0]?["value"]);

            Assert.Equal("Action.Odsl", actions[1]?["type"]?.GetValue<string>());
            Assert.Equal("Open agent settings", actions[1]?["title"]?.GetValue<string>());

            // Verify Action.Compose action
            Assert.Equal("Action.Compose", actions[2]?["type"]?.GetValue<string>());
            Assert.Equal("Ask me a question", actions[2]?["title"]?.GetValue<string>());
            Assert.NotNull(actions[2]?["value"]);
        }

        #endregion

        #region ChannelData Tests

        [Fact]
        public void FromCompatActivity_PreservesChannelData()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                ChannelData = new { customProperty = "customValue", nestedObject = new { key = "value" } }
            };

            CoreActivity coreActivity = activity.FromCompatActivity();

            Assert.NotNull(coreActivity.ChannelData);
            Assert.True(coreActivity.ChannelData.Properties.ContainsKey("customProperty"));
            Assert.Equal("customValue", coreActivity.ChannelData.Properties["customProperty"]?.ToString());
        }

        [Fact]
        public void FromCompatActivity_PreservesComplexChannelData()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromCompatActivity();

            Assert.NotNull(coreActivity.ChannelData);
            Assert.True(coreActivity.ChannelData.Properties.ContainsKey("feedbackLoopEnabled"));

            JsonElement feedbackLoopValue = (JsonElement)coreActivity.ChannelData.Properties["feedbackLoopEnabled"]!;
            Assert.True(feedbackLoopValue.GetBoolean());
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void FromCompatActivity_CompleteRoundTrip_AdaptiveCard()
        {
            // Verify the complete adaptive card payload round-trips successfully
            string originalJson = LoadTestData("AdaptiveCardActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(originalJson)!;

            CoreActivity coreActivity = botActivity.FromCompatActivity();
            string coreActivityJson = coreActivity.ToJson();

            // Use JsonNode.DeepEquals to verify structural equality
            JsonNode originalNode = JsonNode.Parse(originalJson)!;
            JsonNode coreNode = JsonNode.Parse(coreActivityJson)!;

            Assert.True(JsonNode.DeepEquals(originalNode, coreNode));
        }

        [Fact]
        public void FromCompatActivity_CompleteRoundTrip_SuggestedActions()
        {
            // Verify the complete suggested actions payload round-trips successfully
            string originalJson = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(originalJson)!;

            CoreActivity coreActivity = botActivity.FromCompatActivity();
            string coreActivityJson = coreActivity.ToJson();

            // Use JsonNode.DeepEquals to verify structural equality
            JsonNode originalNode = JsonNode.Parse(originalJson)!;
            JsonNode coreNode = JsonNode.Parse(coreActivityJson)!;

            Assert.True(JsonNode.DeepEquals(originalNode, coreNode));
        }

        #endregion

        private static string LoadTestData(string fileName)
        {
            string testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
            return File.ReadAllText(testDataPath);
        }
    }

    public class FromCompatChannelAccountTests
    {
        [Fact]
        public void FromCompatChannelAccount_MapsIdAndName()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1", Name = "Alice" };

            Microsoft.Teams.Bot.Core.Schema.ConversationAccount result = account.FromCompatChannelAccount();

            Assert.Equal("user-1", result.Id);
            Assert.Equal("Alice", result.Name);
        }

        [Fact]
        public void FromCompatChannelAccount_MapsAadObjectIdToProperties()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1", AadObjectId = "aad-123" };

            Microsoft.Teams.Bot.Core.Schema.ConversationAccount result = account.FromCompatChannelAccount();

            Assert.True(result.Properties.TryGetValue("aadObjectId", out object? val));
            Assert.Equal("aad-123", val?.ToString());
        }

        [Fact]
        public void FromCompatChannelAccount_MapsRoleToUserRoleInProperties()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1", Role = "owner" };

            Microsoft.Teams.Bot.Core.Schema.ConversationAccount result = account.FromCompatChannelAccount();

            Assert.True(result.Properties.TryGetValue("userRole", out object? val));
            Assert.Equal("owner", val?.ToString());
        }

        [Fact]
        public void FromCompatChannelAccount_SkipsNullAadObjectIdAndRole()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1" };

            Microsoft.Teams.Bot.Core.Schema.ConversationAccount result = account.FromCompatChannelAccount();

            Assert.False(result.Properties.ContainsKey("aadObjectId"));
            Assert.False(result.Properties.ContainsKey("userRole"));
        }

        [Fact]
        public void FromCompatChannelAccount_ThrowsOnNull()
        {
            Microsoft.Bot.Schema.ChannelAccount? account = null;
            Assert.Throws<ArgumentNullException>(() => account!.FromCompatChannelAccount());
        }
    }

    public class FromCompatConversationParametersTests
    {
        [Fact]
        public void FromCompatConversationParameters_MapsAllScalarFields()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new()
            {
                IsGroup = true,
                TopicName = "Test Topic",
                TenantId = "tenant-abc",
                ChannelData = new { custom = "data" },
            };

            Microsoft.Teams.Bot.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.True(result.IsGroup);
            Assert.Equal("Test Topic", result.TopicName);
            Assert.Equal("tenant-abc", result.TenantId);
            Assert.NotNull(result.ChannelData);
        }

        [Fact]
        public void FromCompatConversationParameters_MapsBotAccount()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new()
            {
                Bot = new Microsoft.Bot.Schema.ChannelAccount { Id = "bot-1", Name = "MyBot" }
            };

            Microsoft.Teams.Bot.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.NotNull(result.Bot);
            Assert.Equal("bot-1", result.Bot.Id);
            Assert.Equal("MyBot", result.Bot.Name);
        }

        [Fact]
        public void FromCompatConversationParameters_MapsMembers()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new()
            {
                Members =
                [
                    new Microsoft.Bot.Schema.ChannelAccount { Id = "user-1", Name = "Alice" },
                    new Microsoft.Bot.Schema.ChannelAccount { Id = "user-2", Name = "Bob" },
                ]
            };

            Microsoft.Teams.Bot.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.NotNull(result.Members);
            Assert.Equal(2, result.Members.Count);
            Assert.Equal("user-1", result.Members[0].Id);
            Assert.Equal("user-2", result.Members[1].Id);
        }

        [Fact]
        public void FromCompatConversationParameters_NullActivityProducesNullActivity()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new() { Activity = null };

            Microsoft.Teams.Bot.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.Null(result.Activity);
        }

        [Fact]
        public void FromCompatConversationParameters_NullBotProducesNullBot()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new() { Bot = null };

            Microsoft.Teams.Bot.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.Null(result.Bot);
        }

        [Fact]
        public void FromCompatConversationParameters_ThrowsOnNull()
        {
            Microsoft.Bot.Schema.ConversationParameters? parameters = null;
            Assert.Throws<ArgumentNullException>(() => parameters!.FromCompatConversationParameters());
        }
    }
}
