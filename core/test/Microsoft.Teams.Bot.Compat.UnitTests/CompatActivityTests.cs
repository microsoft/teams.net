// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Core.Schema;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Teams.Bot.Compat.UnitTests
{
    public class CompatActivityTests
    {
        #region Core Properties Tests

        [Fact]
        public void FromCompatActivity_PreservesCoreProperties()
        {
            var activity = new Activity
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
            Assert.Equal(activity.From.Id, coreActivity.From.Id);
            Assert.Equal(activity.From.Name, coreActivity.From.Name);
            Assert.Equal(activity.Recipient.Id, coreActivity.Recipient.Id);
            Assert.Equal(activity.Conversation.Id, coreActivity.Conversation.Id);
        }

        [Fact]
        public void FromCompatActivity_PreservesTextAndMetadata()
        {
            var activity = new Activity
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

            var attachmentNode = coreActivity.Attachments[0];
            Assert.NotNull(attachmentNode);
            var attachmentObj = attachmentNode.AsObject();

            var contentType = attachmentObj["contentType"]?.GetValue<string>();
            Assert.Equal("application/vnd.microsoft.card.adaptive", contentType);

            var content = attachmentObj["content"];
            Assert.NotNull(content);
            var card = AdaptiveCard.FromJson(content.ToJsonString()).Card;
            Assert.Equal(2, card.Body.Count);
            var firstTextBlock = card.Body[0] as AdaptiveTextBlock;
            Assert.NotNull(firstTextBlock);
            Assert.Equal("Mention a user by User Principle Name: Hello <at>Test User UPN</at>", firstTextBlock.Text);
        }

        [Fact]
        public void FromCompatActivity_PreservesMultipleAttachments()
        {
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                Attachments = new List<Attachment>
                {
                    new Attachment { ContentType = "text/plain", Content = "First attachment" },
                    new Attachment { ContentType = "image/png", ContentUrl = "https://example.com/image.png" }
                }
            };

            CoreActivity coreActivity = activity.FromCompatActivity();

            Assert.NotNull(coreActivity.Attachments);
            Assert.Equal(2, coreActivity.Attachments.Count);
            Assert.Equal("text/plain", coreActivity.Attachments[0]?["contentType"]?.GetValue<string>());
            Assert.Equal("image/png", coreActivity.Attachments[1]?["contentType"]?.GetValue<string>());
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

            var entity = coreActivity.Entities[0]?.AsObject();
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
            Assert.Equal(2, coreActivity.Entities.Count);

            var firstEntity = coreActivity.Entities[0]?.AsObject();
            Assert.Equal("https://schema.org/Message", firstEntity?["type"]?.GetValue<string>());

            var secondEntity = coreActivity.Entities[1]?.AsObject();
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

            var suggestedActions = coreActivityNode["suggestedActions"];
            Assert.NotNull(suggestedActions);

            var actions = suggestedActions["actions"]?.AsArray();
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

            var actions = coreActivityNode["suggestedActions"]?["actions"]?.AsArray();
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
            var activity = new Activity
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

            var feedbackLoopValue = (JsonElement)coreActivity.ChannelData.Properties["feedbackLoopEnabled"]!;
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
}
