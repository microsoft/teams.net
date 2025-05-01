using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Activities;

public class CitationEntityTests
{
    [Fact]
    public void CitationEntity_JsonSerialize()
    {
        var appearance = new CitationAppearance() {
            Name = "doc",
            Abstract = "document abstract",
            Keywords = ["sample", "doc"],
            Text = "full citation text"
        };
        var entity = new CitationEntity()
        {
            Appearance = appearance.ToDocument(),
           Position =2,
            AdditionalType = ["some", "string"]
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/CitationEntity.json"
        ), json);
    }


    [Fact]
    public void CitationEntity_JsonSerialize_Derived()
    {
        var appearance = new CitationAppearance()
        {
            Name = "doc",
            Abstract = "document abstract",
            Keywords = ["sample", "doc"],
            Text = "full citation text"
        };
        CitationEntity entity = new CitationEntity()
        {
            Appearance = appearance.ToDocument(),
            Position = 2,
            AdditionalType = ["some", "string"]
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/CitationEntity.json"
        ), json);
    }  
    
    [Fact]
    public void CitationEntity_JsonSerialize_Interface_Derived()
    {
        var appearance = new CitationAppearance()
        {
            Name = "doc",
            Abstract = "document abstract",
            Keywords = ["sample", "doc"],
            Text = "full citation text"
        };
        Entity entity = new CitationEntity()
        {
            Appearance = appearance.ToDocument(),
            Position = 2,
            AdditionalType = ["some", "string"]
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/CitationEntity.json"
        ), json);
    }


    [Fact]
    public void CitationEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/CitationEntity.json");
        var entity = JsonSerializer.Deserialize<CitationEntity>(json);
        var appearance = new CitationAppearance()
        {
            Name = "doc",
            Abstract = "document abstract",
            Keywords = ["sample", "doc"],
            Text = "full citation text"
        };
        var expected = new CitationEntity()
        {
            Appearance = appearance.ToDocument(),
            Position = 2,
            AdditionalType = ["some", "string"]
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void CitationEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/CitationEntity.json");
        var entity = JsonSerializer.Deserialize<Entity>(json);
        var appearance = new CitationAppearance()
        {
            Name = "doc",
            Abstract = "document abstract",
            Keywords = ["sample", "doc"],
            Text = "full citation text"
        };
        var expected = new CitationEntity()
        {
            Appearance = appearance.ToDocument(),
            Position = 2,
            AdditionalType = ["some", "string"]
        };

        Assert.Equivalent(expected, entity);
    }
}