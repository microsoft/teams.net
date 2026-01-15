// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core.UnitTests.Schema;

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
        Assert.NotNull(activity.Entities);
        Assert.Single(activity.Entities);
        JsonNode? e1 = activity.Entities[0];
        Assert.NotNull(e1);
        Assert.Equal("mention", e1["type"]?.ToString());
        Assert.NotNull(e1["mentioned"]);
        Assert.True(e1["mentioned"]?.AsObject().ContainsKey("id"));
        Assert.NotNull(e1["mentioned"]?["id"]);
        Assert.Equal("user1", e1["mentioned"]?["id"]?.ToString());
        Assert.Equal("User One", e1["mentioned"]?["name"]?.ToString());
        Assert.Equal("<at>User One</at>", e1["text"]?.ToString());
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
        activity.Entities = new JsonArray(nops, mentionEntity);
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
}
