// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Core.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Teams.Apps.BotBuilder.UnitTests
{
    public class ActivitySchemaMapperTests
    {
        #region Core Properties Tests

        [Fact]
        public void FromBotFrameworkActivity_PreservesCoreProperties()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                ServiceUrl = "https://smba.trafficmanager.net/teams",
                ChannelId = "msteams",
                Id = "test-id-123",
                From = new Microsoft.Bot.Schema.ChannelAccount { Id = "user-123", Name = "Test User" },
                Recipient = new Microsoft.Bot.Schema.ChannelAccount { Id = "bot-456", Name = "Test Bot" },
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conv-789", Name = "Test Conversation" }
            };

            CoreActivity coreActivity = activity.FromBotFrameworkActivity();

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
        public void FromBotFrameworkActivity_PreservesTextAndMetadata()
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

            CoreActivity coreActivity = activity.FromBotFrameworkActivity();

            Assert.NotNull(coreActivity);
            Assert.Equal(activity.Text, coreActivity.Properties["text"]?.ToString());
            Assert.Equal(activity.InputHint, coreActivity.Properties["inputHint"]?.ToString());
            Assert.Equal(activity.ReplyToId, coreActivity.ReplyToId);
            Assert.Equal(activity.Locale, coreActivity.Properties["locale"]?.ToString());
        }

        #endregion

        #region Attachments Tests

        [Fact]
        public void FromBotFrameworkActivity_PreservesAdaptiveCardAttachment()
        {
            string json = LoadTestData("AdaptiveCardActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;
            Assert.NotNull(botActivity);
            Assert.Single(botActivity.Attachments);

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();

            Assert.NotNull(coreActivity);
            JsonArray? attachments = coreActivity.Properties.Extract<JsonArray>("attachments");
            Assert.NotNull(attachments);
            Assert.Single(attachments);

            JsonNode? attachmentNode = attachments[0];
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
        public void FromBotFrameworkActivity_PreservesMultipleAttachments()
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

            CoreActivity coreActivity = activity.FromBotFrameworkActivity();

            JsonArray? attachments = coreActivity.Properties.Extract<JsonArray>("attachments");
            Assert.NotNull(attachments);
            Assert.Equal(2, attachments?.Count);
            Assert.Equal("text/plain", attachments?[0]?["contentType"]?.GetValue<string>());
            Assert.Equal("image/png", attachments?[1]?["contentType"]?.GetValue<string>());
        }

        #endregion

        #region Entities Tests

        [Fact]
        public void FromBotFrameworkActivity_PreservesEntities()
        {
            string json = LoadTestData("AdaptiveCardActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();

            JsonArray? entities = coreActivity.Properties.Extract<JsonArray>("entities");
            Assert.NotNull(entities);
            Assert.Single(entities);

            JsonObject? entity = entities[0]?.AsObject();
            Assert.NotNull(entity);
            Assert.Equal("https://schema.org/Message", entity["type"]?.GetValue<string>());
        }

        [Fact]
        public void FromBotFrameworkActivity_PreservesMultipleEntities()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();

            JsonArray? entities = coreActivity.Properties.Extract<JsonArray>("entities");
            Assert.NotNull(entities);
            Assert.Equal(2, entities?.Count);

            JsonObject? firstEntity = entities?[0]?.AsObject();
            Assert.Equal("https://schema.org/Message", firstEntity?["type"]?.GetValue<string>());

            JsonObject? secondEntity = entities?[1]?.AsObject();
            Assert.Equal("BotMessageMetadata", secondEntity?["type"]?.GetValue<string>());
        }

        #endregion

        #region SuggestedActions Tests

        [Fact]
        public void FromBotFrameworkActivity_PreservesSuggestedActions()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;
            Assert.NotNull(botActivity.SuggestedActions);
            Assert.Equal(3, botActivity.SuggestedActions.Actions.Count);

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();

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
        public void FromBotFrameworkActivity_PreservesSuggestedActionDetails()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();
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
        public void FromBotFrameworkActivity_PreservesChannelData()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                ChannelData = new { customProperty = "customValue", nestedObject = new { key = "value" } }
            };

            CoreActivity coreActivity = activity.FromBotFrameworkActivity();

            ChannelData? channelData = coreActivity.Properties.Extract<ChannelData>("channelData");
            Assert.NotNull(channelData);
            Assert.True(channelData.Properties.ContainsKey("customProperty"));
            Assert.Equal("customValue", channelData.Properties["customProperty"]?.ToString());
        }

        [Fact]
        public void FromBotFrameworkActivity_PreservesComplexChannelData()
        {
            string json = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(json)!;

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();

            ChannelData? channelData = coreActivity.Properties.Extract<ChannelData>("channelData");
            Assert.NotNull(channelData);
            Assert.True(channelData.Properties.ContainsKey("feedbackLoopEnabled"));

            JsonElement feedbackLoopValue = (JsonElement)channelData.Properties["feedbackLoopEnabled"]!;
            Assert.True(feedbackLoopValue.GetBoolean());
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void FromBotFrameworkActivity_CompleteRoundTrip_AdaptiveCard()
        {
            // Verify the complete adaptive card payload round-trips successfully
            string originalJson = LoadTestData("AdaptiveCardActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(originalJson)!;

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();
            string coreActivityJson = coreActivity.ToJson();

            // Use JsonNode.DeepEquals to verify structural equality
            JsonNode originalNode = JsonNode.Parse(originalJson)!;
            JsonNode coreNode = JsonNode.Parse(coreActivityJson)!;

            Assert.True(JsonNode.DeepEquals(originalNode, coreNode));
        }

        [Fact]
        public void FromBotFrameworkActivity_CompleteRoundTrip_SuggestedActions()
        {
            // Verify the complete suggested actions payload round-trips successfully
            string originalJson = LoadTestData("SuggestedActionsActivity.json");
            Activity botActivity = JsonConvert.DeserializeObject<Activity>(originalJson)!;

            CoreActivity coreActivity = botActivity.FromBotFrameworkActivity();
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

            Microsoft.Teams.Core.Schema.ChannelAccount result = account.FromCompatChannelAccount();

            Assert.Equal("user-1", result.Id);
            Assert.Equal("Alice", result.Name);
        }

        [Fact]
        public void FromCompatChannelAccount_MapsBotId()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "bot-account-id" };
            account.Properties["botId"] = "28:bot-app-id";

            Microsoft.Teams.Core.Schema.ChannelAccount result = account.FromCompatChannelAccount();

            Assert.Equal("28:bot-app-id", result.BotId);
        }

        [Fact]
        public void FromCompatChannelAccount_MapsAadObjectIdToProperties()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1", AadObjectId = "aad-123" };

            Microsoft.Teams.Core.Schema.ChannelAccount result = account.FromCompatChannelAccount();

            Assert.True(result.Properties.TryGetValue("aadObjectId", out object? val));
            Assert.Equal("aad-123", val?.ToString());
        }

        [Fact]
        public void FromCompatChannelAccount_MapsRoleToUserRoleInProperties()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1", Role = "owner" };

            Microsoft.Teams.Core.Schema.ChannelAccount result = account.FromCompatChannelAccount();

            Assert.True(result.Properties.TryGetValue("userRole", out object? val));
            Assert.Equal("owner", val?.ToString());
        }

        [Fact]
        public void FromCompatChannelAccount_SkipsNullAadObjectIdAndRole()
        {
            Microsoft.Bot.Schema.ChannelAccount account = new() { Id = "user-1" };

            Microsoft.Teams.Core.Schema.ChannelAccount result = account.FromCompatChannelAccount();

            Assert.False(result.Properties.ContainsKey("aadObjectId"));
            Assert.False(result.Properties.ContainsKey("userRole"));
        }

        [Fact]
        public void FromCompatChannelAccount_ThrowsOnNull()
        {
            Microsoft.Bot.Schema.ChannelAccount? account = null;
            Assert.Throws<ArgumentNullException>(() => account!.FromCompatChannelAccount());
        }

        [Fact]
        public void ToCompatChannelAccount_MapsBotId()
        {
            Microsoft.Teams.Core.Schema.ChannelAccount account = new() { Id = "bot-account-id", BotId = "28:bot-app-id" };

            Microsoft.Bot.Schema.ChannelAccount result = account.ToCompatChannelAccount();

            Assert.True(result.Properties.TryGetValue("botId", out JToken? botId));
            Assert.Equal("28:bot-app-id", botId?.ToString());
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

            Microsoft.Teams.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

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

            Microsoft.Teams.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

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

            Microsoft.Teams.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.NotNull(result.Members);
            Assert.Equal(2, result.Members.Count);
            Assert.Equal("user-1", result.Members[0].Id);
            Assert.Equal("user-2", result.Members[1].Id);
        }

        [Fact]
        public void FromCompatConversationParameters_NullActivityProducesNullActivity()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new() { Activity = null };

            Microsoft.Teams.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.Null(result.Activity);
        }

        [Fact]
        public void FromCompatConversationParameters_NullBotProducesNullBot()
        {
            Microsoft.Bot.Schema.ConversationParameters parameters = new() { Bot = null };

            Microsoft.Teams.Core.ConversationParameters result = parameters.FromCompatConversationParameters();

            Assert.Null(result.Bot);
        }

        [Fact]
        public void FromCompatConversationParameters_ThrowsOnNull()
        {
            Microsoft.Bot.Schema.ConversationParameters? parameters = null;
            Assert.Throws<ArgumentNullException>(() => parameters!.FromCompatConversationParameters());
        }

        #region FromBotFrameworkActivityInput Tests

        [Fact]
        public void FromBotFrameworkActivityInput_PreservesTypedProperties()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Id = "act-1",
                ReplyToId = "reply-1",
                Recipient = new Microsoft.Bot.Schema.ChannelAccount { Id = "bot-1", Name = "Bot" }
            };

            CoreActivityInput input = activity.FromBotFrameworkActivityInput();

            Assert.NotNull(input);
            Assert.Equal(ActivityTypes.Message, input.Type);
            Assert.Equal("act-1", input.Id);
            Assert.Equal("reply-1", input.ReplyToId);
            Assert.NotNull(input.Recipient);
            Assert.Equal("bot-1", input.Recipient.Id);
            Assert.Equal("Bot", input.Recipient.Name);
        }

        [Fact]
        public void FromBotFrameworkActivityInput_CarriesRoutingAndIdentityIntoPropertyBag()
        {
            // The outbound input models only type/id/replyToId/recipient as typed members; everything
            // else (routing + body identity) is carried through the extension property bag.
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                ServiceUrl = "https://smba.trafficmanager.net/teams",
                ChannelId = "msteams",
                From = new Microsoft.Bot.Schema.ChannelAccount { Id = "user-1", Name = "User" },
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conv-1", Name = "Conv" }
            };

            CoreActivityInput input = activity.FromBotFrameworkActivityInput();

            Assert.Equal("https://smba.trafficmanager.net/teams", input.Properties["serviceUrl"]?.ToString());
            Assert.Equal("msteams", input.Properties["channelId"]?.ToString());

            JsonElement from = Assert.IsType<JsonElement>(input.Properties["from"]);
            Assert.Equal("user-1", from.GetProperty("id").GetString());
            Assert.Equal("User", from.GetProperty("name").GetString());

            JsonElement conversation = Assert.IsType<JsonElement>(input.Properties["conversation"]);
            Assert.Equal("conv-1", conversation.GetProperty("id").GetString());
            Assert.Equal("Conv", conversation.GetProperty("name").GetString());
        }

        [Fact]
        public void FromBotFrameworkActivityInput_PreservesTextAndMetadata()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "Hello, this is a test message",
                Locale = "en-US",
                InputHint = "acceptingInput"
            };

            CoreActivityInput input = activity.FromBotFrameworkActivityInput();

            Assert.Equal("Hello, this is a test message", input.Properties["text"]?.ToString());
            Assert.Equal("en-US", input.Properties["locale"]?.ToString());
            Assert.Equal("acceptingInput", input.Properties["inputHint"]?.ToString());
        }

        [Fact]
        public void FromBotFrameworkActivityInput_PreservesAttachments()
        {
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Attachments =
                [
                    new Attachment { ContentType = "text/plain", Content = "abc", Name = "f.txt" }
                ]
            };

            CoreActivityInput input = activity.FromBotFrameworkActivityInput();

            JsonElement attachments = Assert.IsType<JsonElement>(input.Properties["attachments"]);
            Assert.Equal(JsonValueKind.Array, attachments.ValueKind);
            Assert.Equal(1, attachments.GetArrayLength());
            JsonElement first = attachments[0];
            Assert.Equal("text/plain", first.GetProperty("contentType").GetString());
            Assert.Equal("abc", first.GetProperty("content").GetString());
            Assert.Equal("f.txt", first.GetProperty("name").GetString());
        }

        [Fact]
        public void FromBotFrameworkActivityInput_RoundTripsConversationThroughJson()
        {
            // Backward compat: the serialized outbound body still surfaces the conversation.
            Activity activity = new()
            {
                Type = ActivityTypes.Message,
                Text = "hi",
                Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conv-42" }
            };

            CoreActivityInput input = activity.FromBotFrameworkActivityInput();
            string json = input.ToJson();

            Assert.Contains("\"conversation\"", json);
            Assert.Contains("\"conv-42\"", json);
        }

        [Fact]
        public void FromBotFrameworkActivityInput_MinimalActivity_MapsTypeOnly()
        {
            Activity activity = new() { Type = ActivityTypes.Typing };

            CoreActivityInput input = activity.FromBotFrameworkActivityInput();

            Assert.Equal(ActivityTypes.Typing, input.Type);
            Assert.Null(input.Id);
            Assert.Null(input.ReplyToId);
            Assert.Null(input.Recipient);
        }

        [Fact]
        public void FromBotFrameworkActivityInput_ThrowsOnNull()
        {
            Activity? activity = null;
            Assert.Throws<ArgumentNullException>(() => activity!.FromBotFrameworkActivityInput());
        }

        #endregion
    }
}
