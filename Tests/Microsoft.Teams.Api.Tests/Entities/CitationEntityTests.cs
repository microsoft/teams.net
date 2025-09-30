using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class CitationEntityTests
{
    [Fact]
    public void CitationEntity_JsonSerialize()
    {
        var appearance = new CitationAppearance()
        {
            Name = "doc",
            Abstract = "document abstract",
            Keywords = ["sample", "doc"],
            Text = "full citation text"
        };
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org",
            AdditionalType = ["some", "string"]
        };
        var entity = new CitationEntity(messageEntity)
        {
            Citation = [new CitationEntity.Claim()
            {
                Position = 2,
                Appearance = appearance.ToDocument()
            }]
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
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org",
            AdditionalType = ["some", "string"]
        };
        CitationEntity entity = new CitationEntity(messageEntity)
        {
            Citation = [new CitationEntity.Claim()
            {
                Position = 2,
                Appearance = appearance.ToDocument()
            }]
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
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org",
            AdditionalType = ["some", "string"]
        };
        Entity entity = new CitationEntity(messageEntity)
        {
            Citation = [new CitationEntity.Claim()
            {
                Position = 2,
                Appearance = appearance.ToDocument()
            }]
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
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org",
            AdditionalType = ["some", "string"]
        };
        var expected = new CitationEntity(messageEntity)
        {
            Citation = [new CitationEntity.Claim()
            {
                Position = 2,
                Appearance = appearance.ToDocument()
            }]
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
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org",
            AdditionalType = ["some", "string"]
        };
        var expected = new CitationEntity(messageEntity)
        {
            Citation = [new CitationEntity.Claim()
            {
                Position = 2,
                Appearance = appearance.ToDocument()
            }]
        };

        Assert.Equivalent(expected, entity);
    }
}