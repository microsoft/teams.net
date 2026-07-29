// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

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
    public void TargetedMessageInfoEntity_RoundTripsThroughSourceGenContext()
    {
        // Pins the [JsonSerializable(typeof(TargetedMessageInfoEntity))] registration on
        // TeamsActivityJsonContext. Without that line, .Default.TargetedMessageInfoEntity wouldn't
        // exist and this test would fail to compile / run — preventing a silent Native AOT regression.
        TargetedMessageInfoEntity entity = new() { MessageId = "1772129782775" };

        string json = JsonSerializer.Serialize(entity, TeamsActivityInputJsonContext.Default.TargetedMessageInfoEntity);
        Assert.Contains("\"messageId\"", json);
        Assert.Contains("1772129782775", json);

        TargetedMessageInfoEntity? roundTripped = JsonSerializer.Deserialize(json, TeamsActivityInputJsonContext.Default.TargetedMessageInfoEntity);
        Assert.NotNull(roundTripped);
        Assert.Equal("targetedMessageInfo", roundTripped.Type);
        Assert.Equal("1772129782775", roundTripped.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_AddsEntity()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("test")
            .WithTargetedMessageInfo("msg-123")
            ;

        TargetedMessageInfoEntity? entity = activity.Entities?.OfType<TargetedMessageInfoEntity>().FirstOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_DoesNotDuplicate_WhenConcreteEntityExists()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("test")
            .AddEntity(new TargetedMessageInfoEntity { MessageId = "9999" })
            .WithTargetedMessageInfo("msg-123")
            ;

        List<TargetedMessageInfoEntity> entities = activity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(entities);
        Assert.Equal("9999", entities[0].MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_DoesNotDuplicate_WhenGenericEntityWithMatchingType()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("test")
            .AddEntity(new Entity("targetedMessageInfo"))
            .WithTargetedMessageInfo("msg-123")
            ;

        List<Entity> entities = activity.Entities!.Where(e => e.Type == "targetedMessageInfo").ToList();
        Assert.Single(entities);
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsQuotedReplyEntities()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("test")
            .AddEntity(new Entity("quotedReply"))
            .WithTargetedMessageInfo("msg-123")
            ;

        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsAllQuotedReplyEntities_WhenMultiplePresent()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("test")
            .AddEntity(new Entity("quotedReply"))
            .AddEntity(new Entity("quotedReply"))
            .AddEntity(new ClientInfoEntity { Locale = "en-us" })
            .WithTargetedMessageInfo("msg-123")
            ;

        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "clientInfo");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsQuotedPlaceholderFromText()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-123", "my response")
            .WithTargetedMessageInfo("msg-123")
            ;
        string? text = activity.Text;
        Assert.Equal("my response", text?.ToString());
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsAllQuotedPlaceholders_NotJustMatchingMessageId()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-1", "first")
            .AddQuote("msg-2", "second")
            .WithTargetedMessageInfo("msg-99")
            ;

        // Passes a different messageId than either existing quote — placeholders for msg-1 and msg-2
        // must still be stripped to keep the activity text consistent with the entity removal.
        string? text = activity.Text;
        Assert.DoesNotContain("<quoted", text?.ToString());
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_OnMessageActivity_AutoPopulatesEntity()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("response")
            .WithTargetedMessageInfo("msg-123")
            ;

        TargetedMessageInfoEntity? entity = activity.Entities?.OfType<TargetedMessageInfoEntity>().FirstOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_LeavesTextUnchanged_WhenNoPlaceholder()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("plain response")
            .WithTargetedMessageInfo("msg-123")
            ;

        string? text = activity.Text;
        Assert.Equal("plain response", text?.ToString());
    }

    [Fact]
    public void AddTargetedMessageInfo_NullText_DoesNotThrow()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithTargetedMessageInfo("msg-123")
            ;

        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_ToJson_ContainsMessageId()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("hello")
            .WithTargetedMessageInfo("msg-123")
            ;

        string json = activity.ToJson();
        Assert.Contains("\"targetedMessageInfo\"", json);
        Assert.Contains("\"messageId\"", json);
        Assert.Contains("msg-123", json);
    }

    // Builder tests: WithTargetedMessageInfo

    [Fact]
    public void Builder_WithTargetedMessageInfo_AddsEntity()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithTargetedMessageInfo("msg-123")
            ;

        TargetedMessageInfoEntity? entity = activity.Entities?.OfType<TargetedMessageInfoEntity>().FirstOrDefault();

        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_ThrowsOnWhitespaceMessageId()
    {
        Assert.Throws<ArgumentException>(() =>
            new MessageActivityInput()
                .WithTargetedMessageInfo("   "));
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_IsIdempotent()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithTargetedMessageInfo("msg-123")
            .WithTargetedMessageInfo("msg-999")
            ;

        List<TargetedMessageInfoEntity> entities = activity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(entities);
        Assert.Equal("msg-123", entities[0].MessageId);
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsQuotedReplyEntities()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-1", "old reply")
            .WithTargetedMessageInfo("msg-123")
            ;
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsPlaceholderFromText()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-123", "my response")
            .WithTargetedMessageInfo("msg-123")
            ;
        string? text = activity.Text;
        Assert.Equal("my response", text?.ToString());
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_ToJson_ContainsMessageId()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("hello")
            .WithTargetedMessageInfo("msg-123")
            ;

        string json = activity.ToJson();
        Assert.Contains("\"targetedMessageInfo\"", json);
        Assert.Contains("\"messageId\"", json);
        Assert.Contains("msg-123", json);
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsEscapedPlaceholder()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("a\"b", "response")
            .WithTargetedMessageInfo("a\"b")
            ;
        string? text = activity.Text;
        Assert.Equal("response", text?.ToString());
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_StripsAllPlaceholders_NotJustMatchingMessageId()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .AddQuote("msg-1", "first")
            .AddQuote("msg-2", "second")
            .WithTargetedMessageInfo("msg-99")
            ;
        string? text = activity.Text;
        Assert.DoesNotContain("<quoted", text?.ToString());
        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void Builder_WithTargetedMessageInfo_OnFreshBuilder()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithTargetedMessageInfo("msg-123")
            ;

        TargetedMessageInfoEntity? entity = activity.Entities?.OfType<TargetedMessageInfoEntity>().FirstOrDefault();

        Assert.Single(activity.Entities!);
        Assert.NotNull(entity);
        Assert.Equal("msg-123", entity.MessageId);
        Assert.True(string.IsNullOrEmpty(activity.Text));
    }
}
