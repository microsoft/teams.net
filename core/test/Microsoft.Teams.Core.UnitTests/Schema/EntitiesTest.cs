// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Schema;

public class EntitiesTest
{
    [Fact]
    public void Test_Entity_Deserialization()
    {
        string json = """
        {
            "type": "message",
            "entities": [
                {
                    "type": "mention",
                    "mentioned": {
                        "id": "user1",
                        "name": "User One"
                    },
                    "text": "<at>User One</at>"
                }
            ]
        }
        """;
        CoreActivity activity = CoreActivity.FromJsonString(json);
        Assert.NotNull(activity);
        Assert.True(activity.Properties.ContainsKey("entities"));
        JsonElement entitiesElement = Assert.IsType<JsonElement>(activity.Properties["entities"]);
        Assert.Equal(JsonValueKind.Array, entitiesElement.ValueKind);
        Assert.Equal(1, entitiesElement.GetArrayLength());
        JsonElement e1 = entitiesElement[0];
        Assert.Equal("mention", e1.GetProperty("type").GetString());
        Assert.True(e1.TryGetProperty("mentioned", out JsonElement mentioned));
        Assert.True(mentioned.TryGetProperty("id", out _));
        Assert.Equal("user1", mentioned.GetProperty("id").GetString());
        Assert.Equal("User One", mentioned.GetProperty("name").GetString());
        Assert.Equal("<at>User One</at>", e1.GetProperty("text").GetString());
    }

    [Fact]
    public void Entitiy_Serialization()
    {
        JsonNodeOptions nops = new()
        {
            PropertyNameCaseInsensitive = false
        };

        CoreActivity activity = new(ActivityType.Message);
        JsonObject mentionEntity = new()
        {
            ["type"] = "mention",
            ["mentioned"] = new JsonObject
            {
                ["id"] = "user1",
                ["name"] = "UserOne"
            },
            ["text"] = "<at>User One</at>"
        };
        activity.Properties["entities"] = new JsonArray(nops, mentionEntity);
        string json = activity.ToJson();
        Assert.NotNull(json);
        Assert.Contains("\"type\": \"mention\"", json);
        Assert.Contains("\"id\": \"user1\"", json);
        Assert.Contains("\"name\": \"UserOne\"", json);
        Assert.Contains("\"text\": \"\\u003Cat\\u003EUser One\\u003C/at\\u003E\"", json);
    }

    [Fact]
    public void Entity_RoundTrip()
    {
        string json = """
        {
            "type": "message",
            "entities": [
                {
                    "type": "mention",
                    "mentioned": {
                        "id": "user1",
                        "name": "User One"
                    },
                    "text": "<at>User One</at>"
                }
            ]
        }
        """;
        CoreActivity activity = CoreActivity.FromJsonString(json);
        string serialized = activity.ToJson();
        Assert.NotNull(serialized);
        Assert.Contains("\"type\": \"mention\"", serialized);
        Assert.Contains("\"id\": \"user1\"", serialized);
        Assert.Contains("\"name\": \"User One\"", serialized);
        Assert.Contains("\"text\": \"\\u003Cat\\u003EUser One\\u003C/at\\u003E\"", serialized);
    }

    [Fact]
    public void Test_Unknown_Entity()
    {
        string json = """
        {
            "type": "message",
            "entities": [
                {
                    "type": "unknownEntityType",
                    "someProperty": "someValue"
                }
            ]
        }
        """;
        CoreActivity activity = CoreActivity.FromJsonString(json);
        Assert.NotNull(activity);
        Assert.True(activity.Properties.ContainsKey("entities"));
        JsonElement entitiesElement = Assert.IsType<JsonElement>(activity.Properties["entities"]);
        Assert.Equal(JsonValueKind.Array, entitiesElement.ValueKind);
        Assert.Equal(1, entitiesElement.GetArrayLength());
        JsonElement e1 = entitiesElement[0];
        Assert.Equal("unknownEntityType", e1.GetProperty("type").GetString());
        Assert.Equal("someValue", e1.GetProperty("someProperty").GetString());
    }
}
