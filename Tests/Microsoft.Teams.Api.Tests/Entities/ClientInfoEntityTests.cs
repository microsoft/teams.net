using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class ClientInfoEntityTests
{
    [Fact]
    public void ClientInfoEntity_JsonSerialize()
    {
        var entity = new ClientInfoEntity()
        {
            Platform = "fakePlatform",
            Locale = "en-US",
            Country = "US",
            Timezone = "GMT-8",
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/ClientInfoEntity.json"
        ), json);
    }


    [Fact]
    public void ClientInfoEntity_JsonSerialize_Derived()
    {
        ClientInfoEntity entity = new ClientInfoEntity()
        {
            Platform = "fakePlatform",
            Locale = "en-US",
            Country = "US",
            Timezone = "GMT-8",
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/ClientInfoEntity.json"
        ), json);
    }

    [Fact]
    public void ClientInfoEntity_JsonSerialize_Interface_Derived()
    {
        IEntity entity = new ClientInfoEntity()
        {
            Platform = "fakePlatform",
            Locale = "en-US",
            Country = "US",
            Timezone = "GMT-8",
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/ClientInfoEntity.json"
        ), json);
    }


    [Fact]
    public void ClientInfoEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/ClientInfoEntity.json");
        var entity = JsonSerializer.Deserialize<ClientInfoEntity>(json);

        var expected = new ClientInfoEntity()
        {
            Platform = "fakePlatform",
            Locale = "en-US",
            Country = "US",
            Timezone = "GMT-8",
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void ClientInfoEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/ClientInfoEntity.json");
        var entity = JsonSerializer.Deserialize<IEntity>(json);
        var expected = new ClientInfoEntity()
        {
            Platform = "fakePlatform",
            Locale = "en-US",
            Country = "US",
            Timezone = "GMT-8",
        };

        Assert.Equivalent(expected, entity);
    }


}