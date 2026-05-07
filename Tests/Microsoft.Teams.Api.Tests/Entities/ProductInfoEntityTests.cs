using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class ProductInfoEntityTests
{
    [Fact]
    public void ProductInfoEntity_JsonSerialize()
    {
        var entity = new ProductInfoEntity()
        {
            Id = "COPILOT"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/ProductInfoEntity.json"
        ), json);
    }

    [Fact]
    public void ProductInfoEntity_JsonSerialize_Derived()
    {
        ProductInfoEntity entity = new ProductInfoEntity()
        {
            Id = "COPILOT"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/ProductInfoEntity.json"
        ), json);
    }

    [Fact]
    public void ProductInfoEntity_JsonSerialize_Interface_Derived()
    {
        Entity entity = new ProductInfoEntity()
        {
            Id = "COPILOT"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/ProductInfoEntity.json"
        ), json);
    }

    [Fact]
    public void ProductInfoEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/ProductInfoEntity.json");
        var entity = JsonSerializer.Deserialize<ProductInfoEntity>(json);

        var expected = new ProductInfoEntity()
        {
            Id = "COPILOT"
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void ProductInfoEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/ProductInfoEntity.json");
        var entity = JsonSerializer.Deserialize<Entity>(json);
        var expected = new ProductInfoEntity()
        {
            Id = "COPILOT"
        };

        Assert.Equivalent(expected, entity);
    }
}