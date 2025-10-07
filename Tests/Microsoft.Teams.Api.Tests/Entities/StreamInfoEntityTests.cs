using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class StreamInfoEntityTests
{
    [Fact]
    public void StreamInfoEntity_JsonSerialize()
    {
        var entity = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("streaming")
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/StreamInfoEntity.json"
        ), json);

        Assert.True(entity.StreamType.IsStreaming);
        Assert.False(entity.StreamType.IsFinal);
        Assert.False(entity.StreamType.IsInformative);
    }

    [Fact]
    public void StreamInfoEntity_ValidateFinalStreamTypes()
    {
        var entity = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("final")
        };

        Assert.True(entity.StreamType.IsFinal);
        Assert.False(entity.StreamType.IsInformative);
        Assert.False(entity.StreamType.IsStreaming);
    }

    [Fact]
    public void StreamInfoEntity_ValidateInformativeStreamTypes()
    {
        var entity = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("informative")
        };

        Assert.True(entity.StreamType.IsInformative);
        Assert.False(entity.StreamType.IsFinal);
        Assert.False(entity.StreamType.IsStreaming);
    }

    [Fact]
    public void StreamInfoEntity_JsonSerialize_Derived()
    {
        StreamInfoEntity entity = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("streaming")
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/StreamInfoEntity.json"
        ), json);


    }

    [Fact]
    public void StreamInfoEntity_JsonSerialize_Interface_Derived()
    {
        IEntity entity = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("streaming")
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/StreamInfoEntity.json"
        ), json);
    }


    [Fact]
    public void StreamInfoEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/StreamInfoEntity.json");
        var entity = JsonSerializer.Deserialize<StreamInfoEntity>(json);

        var expected = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("streaming")
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void StreamInfoEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/StreamInfoEntity.json");
        var entity = JsonSerializer.Deserialize<IEntity>(json);
        var expected = new StreamInfoEntity()
        {
            StreamId = "strId",
            StreamSequence = 3,
            StreamType = new StreamType("streaming")
        };

        Assert.Equivalent(expected, entity);
    }


}