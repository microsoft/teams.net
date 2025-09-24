using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class EntitiesTests
{
    private static IEntity? DeserializeEntity(string json) => JsonSerializer.Deserialize<IEntity>(json);
    private static Entity? DeserializeBase(string json) => JsonSerializer.Deserialize<Entity>(json);
    private static OMessageEntity? DeserializeOMessage(string json) => JsonSerializer.Deserialize<OMessageEntity>(json);

    [Fact]
    public void Entity_MissingType_Throws()
    {
        var json = "{}";
        var ex = Assert.Throws<JsonException>(() => DeserializeEntity(json));
        Assert.Contains("entity must have a 'type'", ex.Message);
    }

    [Fact]
    public void Entity_NullType_Throws()
    {
        var json = "{\"type\":null}";
        var ex = Assert.Throws<JsonException>(() => DeserializeEntity(json));
        Assert.Contains("failed to deserialize entity 'type' property", ex.Message);
    }

    [Fact]
    public void Entity_UnknownType_Throws()
    {
        var json = "{\"type\":\"other\"}";
        var ex = Assert.Throws<JsonException>(() => DeserializeEntity(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }

    // Base Entity converter path
    [Fact]
    public void BaseEntity_Message_DispatchesToMessageEntity()
    {
        var json = "{\"type\":\"message\"}";
        var entity = DeserializeBase(json);
        Assert.NotNull(entity);
    }

    [Fact]
    public void OMessage_MissingOType_Throws()
    {
        var json = "{\"type\":\"https://schema.org/Message\"}";
        var ex = Assert.Throws<JsonException>(() => DeserializeOMessage(json));
        Assert.Contains("must have a '@type'", ex.Message);
    }

    [Fact]
    public void OMessage_NullOType_Throws()
    {
        var json = "{\"type\":\"https://schema.org/Message\",\"@type\":null}";
        var ex = Assert.Throws<JsonException>(() => DeserializeOMessage(json));
        Assert.Contains("failed to deserialize 'https://schema.org/Message' entity '@type' property", ex.Message);
    }

    [Fact]
    public void OMessage_UnknownOType_Throws()
    {
        var json = "{\"type\":\"https://schema.org/Message\",\"@type\":\"Other\"}";
        var ex = Assert.Throws<JsonException>(() => DeserializeOMessage(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }
}