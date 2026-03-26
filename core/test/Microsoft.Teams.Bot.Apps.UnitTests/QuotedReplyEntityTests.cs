// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

#pragma warning disable ExperimentalTeamsQuotedReplies
public class QuotedReplyEntityTests
{
    [Fact]
    public void QuotedReplyEntity_HasCorrectType()
    {
        var entity = new QuotedReplyEntity();
        Assert.Equal("quotedReply", entity.Type);
    }

    [Fact]
    public void QuotedReplyEntity_SetsAndGetsQuotedReply()
    {
        var entity = new QuotedReplyEntity
        {
            QuotedReply = new QuotedReplyData
            {
                MessageId = "msg-123",
                SenderId = "user-1",
                SenderName = "Test User",
                Preview = "Hello, world!",
                Time = "1772050244572",
                IsReplyDeleted = false,
                ValidatedMessageReference = true
            }
        };

        Assert.NotNull(entity.QuotedReply);
        Assert.Equal("msg-123", entity.QuotedReply.MessageId);
        Assert.Equal("user-1", entity.QuotedReply.SenderId);
        Assert.Equal("Test User", entity.QuotedReply.SenderName);
        Assert.Equal("Hello, world!", entity.QuotedReply.Preview);
        Assert.Equal("1772050244572", entity.QuotedReply.Time);
        Assert.False(entity.QuotedReply.IsReplyDeleted);
        Assert.True(entity.QuotedReply.ValidatedMessageReference);
    }

    [Fact]
    public void QuotedReplyEntity_ParameterizedConstructor_SetsMessageId()
    {
        var entity = new QuotedReplyEntity("msg-456");

        Assert.Equal("quotedReply", entity.Type);
        Assert.NotNull(entity.QuotedReply);
        Assert.Equal("msg-456", entity.QuotedReply.MessageId);
    }

    [Fact]
    public void QuotedReplyEntity_MinimalData()
    {
        var entity = new QuotedReplyEntity
        {
            QuotedReply = new QuotedReplyData { MessageId = "msg-1" }
        };

        Assert.NotNull(entity.QuotedReply);
        Assert.Equal("msg-1", entity.QuotedReply.MessageId);
        Assert.Null(entity.QuotedReply.SenderId);
        Assert.Null(entity.QuotedReply.SenderName);
        Assert.Null(entity.QuotedReply.Preview);
        Assert.Null(entity.QuotedReply.Time);
        Assert.Null(entity.QuotedReply.IsReplyDeleted);
        Assert.Null(entity.QuotedReply.ValidatedMessageReference);
    }

    [Fact]
    public void Fixture_QuotedReplyEntity_DeserializesFromJson()
    {
        string json = """
        {
          "type": "message",
          "entities": [
            {
              "type": "quotedReply",
              "quotedReply": {
                "messageId": "1772050244572",
                "senderId": "29:a6cdfb28-56f2-4912-b9c4-2181407c7dde",
                "senderName": "Centralized Test Bot",
                "preview": "Reply from bot.",
                "time": "1772050244572",
                "validatedMessageReference": true
              }
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        var entity = activity.Entities[0] as QuotedReplyEntity;
        Assert.NotNull(entity);
        Assert.Equal("quotedReply", entity.Type);
        Assert.NotNull(entity.QuotedReply);
        Assert.Equal("1772050244572", entity.QuotedReply.MessageId);
        Assert.Equal("29:a6cdfb28-56f2-4912-b9c4-2181407c7dde", entity.QuotedReply.SenderId);
        Assert.Equal("Centralized Test Bot", entity.QuotedReply.SenderName);
        Assert.Equal("Reply from bot.", entity.QuotedReply.Preview);
        Assert.Equal("1772050244572", entity.QuotedReply.Time);
        Assert.True(entity.QuotedReply.ValidatedMessageReference);
    }

    [Fact]
    public void Fixture_QuotedReplyEntity_DeserializesMultipleQuotes()
    {
        string json = """
        {
          "type": "message",
          "text": "<quoted messageId=\"msg-1\"/> first reply <quoted messageId=\"msg-2\"/> second reply",
          "entities": [
            {
              "type": "quotedReply",
              "quotedReply": {
                "messageId": "msg-1",
                "senderName": "User A",
                "preview": "First message"
              }
            },
            {
              "type": "clientInfo",
              "locale": "en-us"
            },
            {
              "type": "quotedReply",
              "quotedReply": {
                "messageId": "msg-2",
                "senderName": "User B",
                "preview": "Second message"
              }
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Equal(3, activity.Entities.Count);

        var quotedReplies = activity.GetQuotedMessages().ToList();
        Assert.Equal(2, quotedReplies.Count);
        Assert.Equal("msg-1", quotedReplies[0].QuotedReply?.MessageId);
        Assert.Equal("msg-2", quotedReplies[1].QuotedReply?.MessageId);
    }

    [Fact]
    public void Fixture_QuotedReplyEntity_DeletedQuote()
    {
        string json = """
        {
          "type": "message",
          "entities": [
            {
              "type": "quotedReply",
              "quotedReply": {
                "messageId": "deleted-msg-1",
                "isReplyDeleted": true
              }
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        var entity = activity.Entities?[0] as QuotedReplyEntity;
        Assert.NotNull(entity);
        Assert.True(entity.QuotedReply?.IsReplyDeleted);
        Assert.Null(entity.QuotedReply?.SenderName);
        Assert.Null(entity.QuotedReply?.Preview);
    }

    // Extension tests: GetQuotedMessages

    [Fact]
    public void GetQuotedMessages_FiltersCorrectly()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
        activity.Entities =
        [
            new ClientInfoEntity { Locale = "en-us" },
            new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = "msg-1" } },
            new MentionEntity(new ConversationAccount { Id = "user-1", Name = "User" }, "<at>User</at>"),
            new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = "msg-2" } }
        ];

        var quotedReplies = activity.GetQuotedMessages().ToList();

        Assert.Equal(2, quotedReplies.Count);
        Assert.Equal("msg-1", quotedReplies[0].QuotedReply?.MessageId);
        Assert.Equal("msg-2", quotedReplies[1].QuotedReply?.MessageId);
    }

    [Fact]
    public void GetQuotedMessages_EmptyWhenNoEntities()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
        activity.Entities = null;

        var quotedReplies = activity.GetQuotedMessages().ToList();

        Assert.Empty(quotedReplies);
    }

    [Fact]
    public void GetQuotedMessages_EmptyWhenNoQuotedReplyEntities()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
        activity.Entities = [new ClientInfoEntity { Locale = "en-us" }];

        var quotedReplies = activity.GetQuotedMessages().ToList();

        Assert.Empty(quotedReplies);
    }

    // Extension tests: AddQuotedReply

    [Fact]
    public void AddQuotedReply_AddsEntityAndPlaceholder()
    {
        MessageActivity activity = new("existing text");
        activity.AddQuotedReply("msg-1");

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<QuotedReplyEntity>(activity.Entities[0]);
        var entity = (QuotedReplyEntity)activity.Entities[0];
        Assert.Equal("msg-1", entity.QuotedReply?.MessageId);
        Assert.Equal("existing text<quoted messageId=\"msg-1\"/>", activity.Text);
    }

    [Fact]
    public void AddQuotedReply_WithResponse_AppendsResponseText()
    {
        MessageActivity activity = new();
        activity.AddQuotedReply("msg-1", "my response");

        Assert.Equal("<quoted messageId=\"msg-1\"/> my response", activity.Text);
    }

    [Fact]
    public void AddQuotedReply_MultiQuoteInterleaved()
    {
        MessageActivity activity = new();
        activity.AddQuotedReply("msg-1", "response to first");
        activity.AddQuotedReply("msg-2", "response to second");

        Assert.Equal(
            "<quoted messageId=\"msg-1\"/> response to first<quoted messageId=\"msg-2\"/> response to second",
            activity.Text);
        Assert.Equal(2, activity.Entities!.Count);
    }

    [Fact]
    public void AddQuotedReply_GroupedQuotes()
    {
        MessageActivity activity = new();
        activity.AddQuotedReply("msg-1");
        activity.AddQuotedReply("msg-2", "response to both");

        Assert.Equal(
            "<quoted messageId=\"msg-1\"/><quoted messageId=\"msg-2\"/> response to both",
            activity.Text);
    }

    [Fact]
    public void AddQuotedReply_EmptyActivity()
    {
        MessageActivity activity = new();
        activity.AddQuotedReply("msg-1");

        Assert.Equal("<quoted messageId=\"msg-1\"/>", activity.Text);
        Assert.Single(activity.Entities!);
    }

    // Builder tests: WithQuotedReply

    [Fact]
    public void Builder_WithQuotedReply_AddsEntityAndPlaceholder()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithQuotedReply("msg-1")
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<QuotedReplyEntity>(activity.Entities[0]);

        // Check text via Properties (builder stores text there)
        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal("<quoted messageId=\"msg-1\"/>", text?.ToString());
    }

    [Fact]
    public void Builder_WithQuotedReply_WithResponse()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithQuotedReply("msg-1", "my response")
            .Build();

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal("<quoted messageId=\"msg-1\"/> my response", text?.ToString());
    }

    [Fact]
    public void AddQuotedReply_Rebase_SurvivesRoundTrip()
    {
        MessageActivity activity = new("hello");
        activity.AddQuotedReply("msg-123", "my response");

        // Verify the base CoreActivity.Entities (JsonArray) contains the quotedReply data
        CoreActivity coreActivity = activity;
        Assert.NotNull(coreActivity.Entities);
        Assert.Single(coreActivity.Entities);

        string? entityJson = coreActivity.Entities[0]?.ToJsonString();
        Assert.NotNull(entityJson);
        Assert.Contains("quotedReply", entityJson);
        Assert.Contains("msg-123", entityJson);
    }

    [Fact]
    public void AddQuotedReply_ToJson_ContainsQuotedReplyData()
    {
        MessageActivity activity = new("hello");
        activity.AddQuotedReply("msg-123", "my response");

        string json = activity.ToJson();
        Assert.Contains("\"quotedReply\"", json);
        Assert.Contains("msg-123", json);
        Assert.Contains("messageId", json);
    }

    [Fact]
    public void Builder_WithQuotedReply_MultipleQuotes()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithQuotedReply("msg-1", "first response")
            .WithQuotedReply("msg-2", "second response")
            .Build();

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal(
            "<quoted messageId=\"msg-1\"/> first response<quoted messageId=\"msg-2\"/> second response",
            text?.ToString());
    }
}
#pragma warning restore ExperimentalTeamsQuotedReplies
