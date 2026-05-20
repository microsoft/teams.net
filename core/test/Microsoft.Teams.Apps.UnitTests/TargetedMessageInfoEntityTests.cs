// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

#pragma warning disable ExperimentalTeamsTargeted
public class TargetedMessageInfoEntityTests
{
    [Fact]
    public void TargetedMessageInfoEntity_HasCorrectType()
    {
        TargetedMessageInfoEntity entity = new() { MessageId = "msg-123" };
        Assert.Equal("targetedMessageInfo", entity.Type);
    }

    [Fact]
    public void TargetedMessageInfoEntity_StoresMessageId()
    {
        TargetedMessageInfoEntity entity = new() { MessageId = "1772129782775" };
        Assert.Equal("1772129782775", entity.MessageId);
    }

    [Fact]
    public void Fixture_TargetedMessageInfoEntity_DeserializesFromJson()
    {
        string json = """
        {
          "type": "message",
          "entities": [
            {
              "type": "targetedMessageInfo",
              "messageId": "1772129782775"
            }
          ]
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        TeamsActivity activity = TeamsActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);

        TargetedMessageInfoEntity? entity = activity.Entities[0] as TargetedMessageInfoEntity;
        Assert.NotNull(entity);
        Assert.Equal("targetedMessageInfo", entity.Type);
        Assert.Equal("1772129782775", entity.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_AddsEntity()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("test")
            .WithTargetedMessageInfo("msg-123")
            .Build();

        TargetedMessageInfoEntity? entity = activity.GetTargetedMessageInfo();
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_ReturnsSameActivity_ForChaining()
    {
        MessageActivity activity = new("test");

        MessageActivity result = activity.AddTargetedMessageInfo("msg-123");

        Assert.Same(activity, result);
    }

    [Fact]
    public void AddTargetedMessageInfo_DoesNotDuplicate_WhenConcreteEntityExists()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("test")
            .AddEntity(new TargetedMessageInfoEntity { MessageId = "9999" })
            .WithTargetedMessageInfo("msg-123")
            .Build();

        List<TargetedMessageInfoEntity> entities = activity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(entities);
        Assert.Equal("9999", entities[0].MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_DoesNotDuplicate_WhenGenericEntityWithMatchingType()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("test")
            .AddEntity(new Entity("targetedMessageInfo"))
            .WithTargetedMessageInfo("msg-123")
            .Build();

        List<Entity> entities = activity.Entities!.Where(e => e.Type == "targetedMessageInfo").ToList();
        Assert.Single(entities);
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsQuotedReplyEntities()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("test")
            .AddEntity(new Entity("quotedReply"))
            .WithTargetedMessageInfo("msg-123")
            .Build();

        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsAllQuotedReplyEntities_WhenMultiplePresent()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("test")
            .AddEntity(new Entity("quotedReply"))
            .AddEntity(new Entity("quotedReply"))
            .AddEntity(new ClientInfoEntity { Locale = "en-us" })
            .WithTargetedMessageInfo("msg-123")
            .Build();

        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "clientInfo");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsQuotedPlaceholderFromText()
    {
#pragma warning disable ExperimentalTeamsQuotedReplies
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .AddQuote("msg-123", "my response")
            .WithTargetedMessageInfo("msg-123")
            .Build();
#pragma warning restore ExperimentalTeamsQuotedReplies

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal("my response", text?.ToString());
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsAllQuotedPlaceholders_NotJustMatchingMessageId()
    {
#pragma warning disable ExperimentalTeamsQuotedReplies
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .AddQuote("msg-1", "first")
            .AddQuote("msg-2", "second")
            .WithTargetedMessageInfo("msg-99")
            .Build();
#pragma warning restore ExperimentalTeamsQuotedReplies

        // Passes a different messageId than either existing quote — placeholders for msg-1 and msg-2
        // must still be stripped to keep the activity text consistent with the entity removal.
        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.DoesNotContain("<quoted", text?.ToString());
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_OnMessageActivity_AutoPopulatesEntity()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("response")
            .WithTargetedMessageInfo("msg-123")
            .Build();

        TargetedMessageInfoEntity? entity = activity.GetTargetedMessageInfo();
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_LeavesTextUnchanged_WhenNoPlaceholder()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("plain response")
            .WithTargetedMessageInfo("msg-123")
            .Build();

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal("plain response", text?.ToString());
    }

    [Fact]
    public void AddTargetedMessageInfo_NullText_DoesNotThrow()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithTargetedMessageInfo("msg-123")
            .Build();

        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_ToJson_ContainsMessageId()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("hello")
            .WithTargetedMessageInfo("msg-123")
            .Build();

        string json = activity.ToJson();
        Assert.Contains("\"targetedMessageInfo\"", json);
        Assert.Contains("\"messageId\"", json);
        Assert.Contains("msg-123", json);
    }

    [Fact]
    public void AddTargetedMessageInfo_ThrowsOnNullActivity()
    {
        MessageActivity? activity = null;
        Assert.Throws<ArgumentNullException>(() => activity!.AddTargetedMessageInfo("msg-123"));
    }

    [Fact]
    public void AddTargetedMessageInfo_ThrowsOnWhitespaceMessageId()
    {
        MessageActivity activity = new("test");
        Assert.Throws<ArgumentException>(() => activity.AddTargetedMessageInfo("   "));
    }

    // Builder tests: WithTargetedMessageInfo

    [Fact]
    public void Builder_WithTargetedMessageInfo_AddsEntity()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithTargetedMessageInfo("msg-123")
            .Build();

        TargetedMessageInfoEntity? entity = activity.GetTargetedMessageInfo();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_ThrowsOnNonMessageType()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TeamsActivity.CreateBuilder()
                .WithType(TeamsActivityType.Typing)
                .WithTargetedMessageInfo("msg-123"));
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_ThrowsOnWhitespaceMessageId()
    {
        Assert.Throws<ArgumentException>(() =>
            TeamsActivity.CreateBuilder()
                .WithType(TeamsActivityType.Message)
                .WithTargetedMessageInfo("   "));
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_IsIdempotent()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithTargetedMessageInfo("msg-123")
            .WithTargetedMessageInfo("msg-999")
            .Build();

        List<TargetedMessageInfoEntity> entities = activity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(entities);
        Assert.Equal("msg-123", entities[0].MessageId);
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsQuotedReplyEntities()
    {
#pragma warning disable ExperimentalTeamsQuotedReplies
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .AddQuote("msg-1", "old reply")
            .WithTargetedMessageInfo("msg-123")
            .Build();
#pragma warning restore ExperimentalTeamsQuotedReplies

        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsPlaceholderFromText()
    {
#pragma warning disable ExperimentalTeamsQuotedReplies
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .AddQuote("msg-123", "my response")
            .WithTargetedMessageInfo("msg-123")
            .Build();
#pragma warning restore ExperimentalTeamsQuotedReplies

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal("my response", text?.ToString());
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_ToJson_ContainsMessageId()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithText("hello")
            .WithTargetedMessageInfo("msg-123")
            .Build();

        string json = activity.ToJson();
        Assert.Contains("\"targetedMessageInfo\"", json);
        Assert.Contains("\"messageId\"", json);
        Assert.Contains("msg-123", json);
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsEscapedPlaceholder()
    {
#pragma warning disable ExperimentalTeamsQuotedReplies
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .AddQuote("a\"b", "response")
            .WithTargetedMessageInfo("a\"b")
            .Build();
#pragma warning restore ExperimentalTeamsQuotedReplies

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.Equal("response", text?.ToString());
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsAllPlaceholders_NotJustMatchingMessageId()
    {
#pragma warning disable ExperimentalTeamsQuotedReplies
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .AddQuote("msg-1", "first")
            .AddQuote("msg-2", "second")
            .WithTargetedMessageInfo("msg-99")
            .Build();
#pragma warning restore ExperimentalTeamsQuotedReplies

        Assert.True(activity.Properties.TryGetValue("text", out object? text));
        Assert.DoesNotContain("<quoted", text?.ToString());
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_OnFreshBuilder()
    {
        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithTargetedMessageInfo("msg-123")
            .Build();

        TargetedMessageInfoEntity? entity = activity.GetTargetedMessageInfo();

        Assert.Single(activity.Entities!);
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
        Assert.False(activity.Properties.ContainsKey("text"));
    }
}
#pragma warning restore ExperimentalTeamsTargeted
