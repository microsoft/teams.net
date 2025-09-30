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

    [Fact]
    public void CitationEntity_CreatedFromMessageEntity()
    {
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org",
            AdditionalType = ["AIGeneratedContent"]
        };

        var citationEntity = new CitationEntity(messageEntity);

        Assert.Equal(messageEntity.Type, citationEntity.Type);
        Assert.Equal(messageEntity.OType, citationEntity.OType);
        Assert.Equal(messageEntity.OContext, citationEntity.OContext);
        Assert.Equal(messageEntity.AdditionalType, citationEntity.AdditionalType);
    }

    [Fact]
    public void CitationEntity_AddsCitation()
    {
        var messageEntity = new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org"
        };

        var citationEntity = new CitationEntity(messageEntity);
        citationEntity.Citation ??= [];
        citationEntity.Citation.Add(new CitationEntity.Claim()
        {
            Position = 1,
            Appearance = new CitationEntity.AppearanceDocument()
            {
                Name = "Test Document",
                Abstract = "Test abstract"
            }
        });

        Assert.NotNull(citationEntity.Citation);
        Assert.Single(citationEntity.Citation);
        Assert.Equal(1, citationEntity.Citation[0].Position);
        Assert.Equal("Test Document", citationEntity.Citation[0].Appearance.Name);
        Assert.Equal("Test abstract", citationEntity.Citation[0].Appearance.Abstract);
    }

    [Fact]
    public void CitationEntity_PreservesExistingCitations()
    {
        var existingCitation = new CitationEntity.Claim()
        {
            Position = 1,
            Appearance = new CitationEntity.AppearanceDocument()
            {
                Name = "Existing Doc",
                Abstract = "Existing abstract"
            }
        };

        var messageEntity = new CitationEntity(new MessageEntity()
        {
            Type = "https://schema.org/Message",
            OType = "Message",
            OContext = "https://schema.org"
        });
        messageEntity.Citation = [existingCitation];

        var citationEntity = new CitationEntity(messageEntity);

        Assert.NotNull(citationEntity.Citation);
        Assert.Single(citationEntity.Citation);
        Assert.Equal(1, citationEntity.Citation[0].Position);
        Assert.Equal("Existing Doc", citationEntity.Citation[0].Appearance.Name);
    }


}