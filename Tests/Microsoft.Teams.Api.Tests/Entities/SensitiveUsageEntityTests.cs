using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class SensitiveUsageEntityTests
{
    [Fact]
    public void SensitiveUsageEntity_JsonSerialize()
    {
        var entity = new SensitiveUsageEntity()
        {
            Name = "A1",
            Description = "desc valid",
            Pattern = new DefinedTerm() { Name = "T1", TermCode = "code", InDefinedTermSet = "termSet" },
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/SensitiveUsageEntity.json"
        ), json);
    }


    [Fact]
    public void SensitiveUsageEntity_JsonSerialize_Derived()
    {
        SensitiveUsageEntity entity = new SensitiveUsageEntity()
        {
            Name = "A1",
            Description = "desc valid",
            Pattern = new DefinedTerm() { Name = "T1", TermCode = "code", InDefinedTermSet = "termSet" },
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/SensitiveUsageEntity.json"
        ), json);
    }

    [Fact]
    public void SensitiveUsageEntity_JsonSerialize_Interface_Derived()
    {
        Entity entity = new SensitiveUsageEntity()
        {
            Name = "A1",
            Description = "desc valid",
            Pattern = new DefinedTerm() { Name = "T1", TermCode = "code", InDefinedTermSet = "termSet" },
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/SensitiveUsageEntity.json"
        ), json);
    }


    [Fact]
    public void SensitiveUsageEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/SensitiveUsageEntity.json");
        var entity = JsonSerializer.Deserialize<SensitiveUsageEntity>(json);

        var expected = new SensitiveUsageEntity()
        {
            Name = "A1",
            Description = "desc valid",
            Pattern = new DefinedTerm() { Name = "T1", TermCode = "code", InDefinedTermSet = "termSet" },
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void SensitiveUsageEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/SensitiveUsageEntity.json");
        var entity = JsonSerializer.Deserialize<Entity>(json);
        var expected = new SensitiveUsageEntity()
        {
            Name = "A1",
            Description = "desc valid",
            Pattern = new DefinedTerm() { Name = "T1", TermCode = "code", InDefinedTermSet = "termSet" },
        };

        Assert.Equivalent(expected, entity);
    }


}