// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class QuotedReplyEntityTests
{
    [Fact]
    public void QuotedReplyEntity_HasCorrectType()
    {
        QuotedReplyEntity entity = new();
        Assert.Equal("quotedReply", entity.Type);
    }

    [Fact]
    public void QuotedReplyEntity_SetsAndGetsQuotedReply()
    {
        QuotedReplyEntity entity = new()
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
        QuotedReplyEntity entity = new("msg-456");

        Assert.Equal("quotedReply", entity.Type);
        Assert.NotNull(entity.QuotedReply);
        Assert.Equal("msg-456", entity.QuotedReply.MessageId);
    }

    [Fact]
    public void QuotedReplyEntity_MinimalData()
    {
        QuotedReplyEntity entity = new()
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

        QuotedReplyEntity? entity = activity.Entities[0] as QuotedReplyEntity;
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

        List<QuotedReplyEntity> quotedReplies = activity.GetQuotedMessages().ToList();
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

        QuotedReplyEntity? entity = activity.Entities?[0] as QuotedReplyEntity;
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
            new MentionEntity(new ChannelAccount { Id = "user-1", Name = "User" }, "<at>User</at>"),
            new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = "msg-2" } }
        ];

        List<QuotedReplyEntity> quotedReplies = activity.GetQuotedMessages().ToList();

        Assert.Equal(2, quotedReplies.Count);
        Assert.Equal("msg-1", quotedReplies[0].QuotedReply?.MessageId);
        Assert.Equal("msg-2", quotedReplies[1].QuotedReply?.MessageId);
    }

    [Fact]
    public void GetQuotedMessages_EmptyWhenNoEntities()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
        activity.Entities = null;

        List<QuotedReplyEntity> quotedReplies = activity.GetQuotedMessages().ToList();

        Assert.Empty(quotedReplies);
    }

    [Fact]
    public void GetQuotedMessages_EmptyWhenNoQuotedReplyEntities()
    {
        TeamsActivity activity = TeamsActivity.FromActivity(new CoreActivity(ActivityType.Message));
        activity.Entities = [new ClientInfoEntity { Locale = "en-us" }];

        List<QuotedReplyEntity> quotedReplies = activity.GetQuotedMessages().ToList();

        Assert.Empty(quotedReplies);
    }

    // Extension tests: AddQuote

    [Fact]
    public void AddQuote_AddsEntityAndPlaceholder()
    {
        MessageActivityInput activity = new MessageActivityInput().WithText("existing text").AddQuote("msg-1");

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<QuotedReplyEntity>(activity.Entities[0]);
        QuotedReplyEntity entity = (QuotedReplyEntity)activity.Entities[0];
        Assert.Equal("msg-1", entity.QuotedReply?.MessageId);
        Assert.Equal("existing text<quoted messageId=\"msg-1\"/>", activity.Text);
    }

    [Fact]
    public void AddQuote_WithResponse_AppendsResponseText()
    {
        MessageActivityInput activity = new MessageActivityInput().AddQuote("msg-1", "my response");

        Assert.Equal("<quoted messageId=\"msg-1\"/> my response", activity.Text);
    }

    [Fact]
    public void AddQuote_MultiQuoteInterleaved()
    {
        MessageActivityInput activity = new MessageActivityInput().AddQuote("msg-1", "response to first").AddQuote("msg-2", "response to second");

        Assert.Equal(
            "<quoted messageId=\"msg-1\"/> response to first<quoted messageId=\"msg-2\"/> response to second",
            activity.Text);
        Assert.Equal(2, activity.Entities!.Count);
    }

    [Fact]
    public void AddQuote_GroupedQuotes()
    {
        MessageActivityInput activity = new MessageActivityInput().AddQuote("msg-1").AddQuote("msg-2", "response to both");

        Assert.Equal(
            "<quoted messageId=\"msg-1\"/><quoted messageId=\"msg-2\"/> response to both",
            activity.Text);
    }

    [Fact]
    public void AddQuote_EmptyActivity()
    {
        MessageActivityInput activity = new MessageActivityInput().AddQuote("msg-1");

        Assert.Equal("<quoted messageId=\"msg-1\"/>", activity.Text);
        Assert.Single(activity.Entities!);
    }

    // Builder tests: WithQuote

    [Fact]
    public void Builder_WithQuote_AddsEntityAndPlaceholder()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-1")
            ;

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.IsType<QuotedReplyEntity>(activity.Entities[0]);

        // Check text via Properties (builder stores text there)
        string? text = activity.Text;
        Assert.Equal("<quoted messageId=\"msg-1\"/>", text?.ToString());
    }

    [Fact]
    public void Builder_WithQuote_WithResponse()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-1", "my response")
            ;

        string? text = activity.Text;
        Assert.Equal("<quoted messageId=\"msg-1\"/> my response", text?.ToString());
    }

    [Fact]
    public void AddQuote_ToJson_ContainsQuotedReplyData()
    {
        MessageActivityInput activity = new MessageActivityInput().WithText("hello").AddQuote("msg-123", "my response");

        string json = activity.ToJson();
        Assert.Contains("\"quotedReply\"", json);
        Assert.Contains("msg-123", json);
        Assert.Contains("messageId", json);
    }

    // Extension tests: PrependQuote

    [Fact]
    public void PrependQuote_EmptyText_SetsPlaceholderOnly()
    {
        MessageActivityInput activity = new MessageActivityInput().PrependQuote("msg-1");

        Assert.Equal("<quoted messageId=\"msg-1\"/>", activity.Text);
        Assert.Single(activity.Entities!);
    }

    [Fact]
    public void PrependQuote_NonEmptyText_PrependsPlaceholderWithSpace()
    {
        MessageActivityInput activity = new MessageActivityInput().WithText("hello world").PrependQuote("msg-1");

        Assert.Equal("<quoted messageId=\"msg-1\"/> hello world", activity.Text);
    }

    [Fact]
    public void PrependQuote_TrimsExistingText()
    {
        MessageActivityInput activity = new MessageActivityInput().WithText("   hello   ").PrependQuote("msg-1");

        Assert.Equal("<quoted messageId=\"msg-1\"/> hello", activity.Text);
    }

    [Fact]
    public void PrependQuote_InsertsEntityAtIndexZero()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("existing")
            .WithEntities([new ClientInfoEntity { Locale = "en-us" }])
            .PrependQuote("msg-1")
            ;

        Assert.Equal(2, activity.Entities!.Count);
        Assert.IsType<QuotedReplyEntity>(activity.Entities[0]);
        Assert.IsType<ClientInfoEntity>(activity.Entities[1]);
        Assert.Equal("msg-1", ((QuotedReplyEntity)activity.Entities[0]).QuotedReply?.MessageId);
    }

    // Escaping tests

    [Fact]
    public void AddQuote_EscapesSpecialCharsInPlaceholder()
    {
        MessageActivityInput activity = new MessageActivityInput().AddQuote("msg<\"&>1");

        // Placeholder uses XML-escaped attribute value; entity carries raw id
        Assert.Equal("<quoted messageId=\"msg&lt;&quot;&amp;&gt;1\"/>", activity.Text);
        QuotedReplyEntity entity = (QuotedReplyEntity)activity.Entities![0];
        Assert.Equal("msg<\"&>1", entity.QuotedReply?.MessageId);
    }

    [Fact]
    public void Builder_WithQuote_EscapesSpecialCharsInPlaceholder()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("a\"b")
            ;

        string? text = activity.Text;
        Assert.Equal("<quoted messageId=\"a&quot;b\"/>", text?.ToString());
    }

    [Fact]
    public void Builder_WithQuote_MultipleQuotes()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-1", "first response")
            .AddQuote("msg-2", "second response")
            ;

        Assert.NotNull(activity.Entities);
        Assert.Equal(2, activity.Entities.Count);

        string? text = activity.Text;
        Assert.Equal(
            "<quoted messageId=\"msg-1\"/> first response<quoted messageId=\"msg-2\"/> second response",
            text?.ToString());
    }
}
