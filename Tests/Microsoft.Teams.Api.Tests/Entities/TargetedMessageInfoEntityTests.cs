using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

#pragma warning disable ExperimentalTeamsTargeted
public class TargetedMessageInfoEntityTests
{
    [Fact]
    public void TargetedMessageInfoEntity_JsonSerialize()
    {
        var entity = new TargetedMessageInfoEntity()
        {
            MessageId = "1772129782775"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/TargetedMessageInfoEntity.json"
        ), json);
    }

    [Fact]
    public void TargetedMessageInfoEntity_JsonSerialize_Derived()
    {
        Entity entity = new TargetedMessageInfoEntity()
        {
            MessageId = "1772129782775"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/TargetedMessageInfoEntity.json"
        ), json);
    }

    [Fact]
    public void TargetedMessageInfoEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/TargetedMessageInfoEntity.json");
        var entity = JsonSerializer.Deserialize<TargetedMessageInfoEntity>(json);

        Assert.NotNull(entity);
        Assert.Equal("targetedMessageInfo", entity.Type);
        Assert.Equal("1772129782775", entity.MessageId);
    }

    [Fact]
    public void TargetedMessageInfoEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/TargetedMessageInfoEntity.json");
        var entity = JsonSerializer.Deserialize<Entity>(json);

        Assert.NotNull(entity);
        Assert.IsType<TargetedMessageInfoEntity>(entity);

        var targeted = (TargetedMessageInfoEntity)entity;
        Assert.Equal("targetedMessageInfo", targeted.Type);
        Assert.Equal("1772129782775", targeted.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_AddsEntity()
    {
        var activity = new MessageActivity("test");
        activity.AddTargetedMessageInfo("12345");

        var entity = activity.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("12345", entity!.MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_DoesNotDuplicate_WhenConcreteEntityExists()
    {
        var activity = new MessageActivity("test")
            .AddEntity(new TargetedMessageInfoEntity { MessageId = "9999" });

        activity.AddTargetedMessageInfo("12345");

        var entities = activity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(entities);
        Assert.Equal("9999", entities[0].MessageId);
    }

    [Fact]
    public void AddTargetedMessageInfo_DoesNotDuplicate_WhenGenericEntityWithMatchingType()
    {
        var activity = new MessageActivity("test")
            .AddEntity(new Entity("targetedMessageInfo"));

        activity.AddTargetedMessageInfo("12345");

        var entities = activity.Entities!.Where(e => e.Type == "targetedMessageInfo").ToList();
        Assert.Single(entities);
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsQuotedReplyEntities()
    {
        var activity = new MessageActivity("test")
            .AddEntity(new Entity("quotedReply"));

        activity.AddTargetedMessageInfo("12345");

        Assert.DoesNotContain(activity.Entities!, e => e.Type == "quotedReply");
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }

    [Fact]
    public void AddTargetedMessageInfo_StripsQuotedPlaceholderFromText()
    {
        var activity = new MessageActivity("<quoted messageId=\"12345\"/> Here is my reply");

        activity.AddTargetedMessageInfo("12345");

        Assert.Equal("Here is my reply", activity.Text);
        Assert.Contains(activity.Entities!, e => e.Type == "targetedMessageInfo");
    }
}
