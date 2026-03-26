using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class QuotedReplyEntityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IndentSize = 2,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void QuotedReplyEntity_JsonSerialize()
    {
        var entity = new QuotedReplyEntity()
        {
            QuotedReply = new QuotedReplyData()
            {
                MessageId = "1234567890",
                SenderId = "user-1",
                SenderName = "Test User",
                Preview = "Hello, world!",
                Time = "1772050244572",
                IsReplyDeleted = false,
                ValidatedMessageReference = true
            }
        };

        var json = JsonSerializer.Serialize(entity, JsonOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/QuotedReplyEntity.json"
        ), json);
    }

    [Fact]
    public void QuotedReplyEntity_JsonSerialize_Derived()
    {
        Entity entity = new QuotedReplyEntity()
        {
            QuotedReply = new QuotedReplyData()
            {
                MessageId = "1234567890",
                SenderId = "user-1",
                SenderName = "Test User",
                Preview = "Hello, world!",
                Time = "1772050244572",
                IsReplyDeleted = false,
                ValidatedMessageReference = true
            }
        };

        var json = JsonSerializer.Serialize(entity, JsonOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/QuotedReplyEntity.json"
        ), json);
    }

    [Fact]
    public void QuotedReplyEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/QuotedReplyEntity.json");
        var entity = JsonSerializer.Deserialize<QuotedReplyEntity>(json);

        Assert.NotNull(entity);
        Assert.Equal("quotedReply", entity.Type);
        Assert.NotNull(entity.QuotedReply);
        Assert.Equal("1234567890", entity.QuotedReply.MessageId);
        Assert.Equal("user-1", entity.QuotedReply.SenderId);
        Assert.Equal("Test User", entity.QuotedReply.SenderName);
        Assert.Equal("Hello, world!", entity.QuotedReply.Preview);
        Assert.Equal("1772050244572", entity.QuotedReply.Time);
        Assert.Equal(false, entity.QuotedReply.IsReplyDeleted);
        Assert.Equal(true, entity.QuotedReply.ValidatedMessageReference);
    }

    [Fact]
    public void QuotedReplyEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/QuotedReplyEntity.json");
        var entity = JsonSerializer.Deserialize<Entity>(json);

        Assert.NotNull(entity);
        Assert.IsType<QuotedReplyEntity>(entity);
        var quotedReply = (QuotedReplyEntity)entity;
        Assert.Equal("quotedReply", quotedReply.Type);
        Assert.NotNull(quotedReply.QuotedReply);
        Assert.Equal("1234567890", quotedReply.QuotedReply.MessageId);
    }

    [Fact]
    public void QuotedReplyEntity_RoundTrip()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/QuotedReplyEntity.json");
        var entity = JsonSerializer.Deserialize<QuotedReplyEntity>(json);
        var reserialized = JsonSerializer.Serialize(entity, JsonOptions);

        Assert.Equal(json, reserialized);
    }

    [Fact]
    public void QuotedReplyEntity_MinimalData()
    {
        var entity = new QuotedReplyEntity()
        {
            QuotedReply = new QuotedReplyData()
            {
                MessageId = "msg-1"
            }
        };

        var json = JsonSerializer.Serialize(entity, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<QuotedReplyEntity>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("quotedReply", deserialized.Type);
        Assert.NotNull(deserialized.QuotedReply);
        Assert.Equal("msg-1", deserialized.QuotedReply.MessageId);
        Assert.Null(deserialized.QuotedReply.SenderId);
        Assert.Null(deserialized.QuotedReply.SenderName);
        Assert.Null(deserialized.QuotedReply.Preview);
        Assert.Null(deserialized.QuotedReply.Time);
        Assert.Null(deserialized.QuotedReply.IsReplyDeleted);
        Assert.Null(deserialized.QuotedReply.ValidatedMessageReference);
    }

    [Fact]
    public void GetQuotedMessages_Getter_FiltersCorrectly()
    {
        var message = new MessageActivity("test");
        message.Entities = new List<IEntity>
        {
            new ClientInfoEntity() { Locale = "en-us" },
            new QuotedReplyEntity()
            {
                QuotedReply = new QuotedReplyData() { MessageId = "msg-1" }
            },
            new MentionEntity()
            {
                Mentioned = new Account() { Id = "user-1", Name = "User" },
                Text = "<at>User</at>"
            },
            new QuotedReplyEntity()
            {
                QuotedReply = new QuotedReplyData() { MessageId = "msg-2" }
            }
        };

        var quotedReplies = message.GetQuotedMessages();

        Assert.Equal(2, quotedReplies.Count);
        Assert.Equal("msg-1", quotedReplies[0].QuotedReply?.MessageId);
        Assert.Equal("msg-2", quotedReplies[1].QuotedReply?.MessageId);
    }

    [Fact]
    public void GetQuotedMessages_Getter_EmptyWhenNoEntities()
    {
        var message = new MessageActivity("test");
        message.Entities = null;

        var quotedReplies = message.GetQuotedMessages();

        Assert.Empty(quotedReplies);
    }

    [Fact]
    public void GetQuotedMessages_Getter_EmptyWhenNoQuotedReplyEntities()
    {
        var message = new MessageActivity("test");
        message.Entities = new List<IEntity>
        {
            new ClientInfoEntity() { Locale = "en-us" }
        };

        var quotedReplies = message.GetQuotedMessages();

        Assert.Empty(quotedReplies);
    }

    [Fact]
    public void AddQuote_AddsEntityAndPlaceholder()
    {
        var message = new MessageActivity().AddQuote("msg-1");

        Assert.Single(message.Entities!);
        Assert.Equal("quotedReply", message.Entities![0].Type);
        Assert.Contains("<quoted messageId=\"msg-1\"/>", message.Text);
    }

    [Fact]
    public void AddQuote_WithResponse_AppendsResponseText()
    {
        var message = new MessageActivity().AddQuote("msg-1", "my response");

        Assert.Equal("<quoted messageId=\"msg-1\"/> my response", message.Text);
    }

    [Fact]
    public void AddQuote_MultiQuoteInterleaved()
    {
        var message = new MessageActivity()
            .AddQuote("msg-1", "response to first")
            .AddQuote("msg-2", "response to second");

        Assert.Equal(
            "<quoted messageId=\"msg-1\"/> response to first<quoted messageId=\"msg-2\"/> response to second",
            message.Text);
        Assert.Equal(2, message.Entities!.Count);
    }

    [Fact]
    public void AddQuote_GroupedQuotes()
    {
        var message = new MessageActivity()
            .AddQuote("msg-1")
            .AddQuote("msg-2", "response to both");

        Assert.Equal(
            "<quoted messageId=\"msg-1\"/><quoted messageId=\"msg-2\"/> response to both",
            message.Text);
    }

    [Fact]
#pragma warning disable CS0618 // Obsolete
    public void ToQuoteReply_ReturnsModernPlaceholder()
    {
        var message = new MessageActivity("test") { Id = "activity-123" };

        var result = message.ToQuoteReply();

        Assert.Equal("<quoted messageId=\"activity-123\"/>", result);
    }
#pragma warning restore CS0618

    [Fact]
#pragma warning disable CS0618 // Obsolete
    public void ToQuoteReply_ReturnsEmptyWhenNoId()
    {
        var message = new MessageActivity("test");

        var result = message.ToQuoteReply();

        Assert.Equal(string.Empty, result);
    }
#pragma warning restore CS0618
}