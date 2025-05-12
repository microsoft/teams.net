using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class MessageEntityTests
{
    [Fact]
    public void MessageEntity_JsonSerialize()
    {

        var entity = new MessageEntity()
        {
            AdditionalType = ["test", "valid"]
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/MessageEntity.json"
        ), json);
    }


    [Fact]
    public void MessageEntity_JsonSerialize_Derived()
    {
        MessageEntity entity = new MessageEntity()
        {
            AdditionalType = ["test", "valid"]
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/MessageEntity.json"
        ), json);
    }  
    
    [Fact]
    public void MessageEntity_JsonSerialize_Interface_Derived()
    {
        Entity entity = new MessageEntity()
        {
            AdditionalType = ["test", "valid"]
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/MessageEntity.json"
        ), json);
    }


    [Fact]
    public void MessageEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/MessageEntity.json");
        var entity = JsonSerializer.Deserialize<MessageEntity>(json);
        var expected = new MessageEntity()
        {
            AdditionalType = ["test", "valid"]
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void MessageEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/MessageEntity.json");
        var entity = JsonSerializer.Deserialize<Entity>(json);
      
        var expected = new MessageEntity()
        {
            AdditionalType = ["test", "valid"]
        };

        Assert.Equivalent(expected, entity);
    }


}