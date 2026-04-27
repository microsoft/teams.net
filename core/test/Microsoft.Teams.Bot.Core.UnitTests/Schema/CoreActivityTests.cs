// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core.UnitTests.Schema;

public class CoreCoreActivityTests
{
    [Fact]
    public void Ctor_And_Nulls()
    {
        CoreActivity a1 = new();
        Assert.NotNull(a1);
        Assert.Equal(ActivityType.Message, a1.Type);

        CoreActivity a2 = new()
        {
            Type = "mytype"
        };
        Assert.NotNull(a2);
        Assert.Equal("mytype", a2.Type);
    }

    [Fact]
    public void Json_Nulls_Not_Deserialized()
    {
        string json = """
        {
            "type": "message",
            "text": null
        }
        """;
        CoreActivity act = CoreActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("message", act.Type);

        string json2 = """
        {
            "type": "message"
        }
        """;
        CoreActivity act2 = CoreActivity.FromJsonString(json2);
        Assert.NotNull(act2);
        Assert.Equal("message", act2.Type);

    }

    [Fact]
    public void Accept_Unkown_Primitive_Fields()
    {
        string json = """
        {
            "type": "message",
            "text": "hello",
            "unknownString": "some string",
            "unknownInt": 123,
            "unknownBool": true,
            "unknownNull": null
        }
        """;
        CoreActivity act = CoreActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("message", act.Type);
        Assert.True(act.Properties.ContainsKey("unknownString"));
        Assert.True(act.Properties.ContainsKey("unknownInt"));
        Assert.True(act.Properties.ContainsKey("unknownBool"));
        Assert.True(act.Properties.ContainsKey("unknownNull"));
        Assert.Equal("some string", act.Properties["unknownString"]?.ToString());
        Assert.Equal(123, ((JsonElement)act.Properties["unknownInt"]!).GetInt32());
        Assert.True(((JsonElement)act.Properties["unknownBool"]!).GetBoolean());
        Assert.Null(act.Properties["unknownNull"]);
    }

    [Fact]
    public void Serialize_Unkown_Primitive_Fields()
    {
        CoreActivity act = new()
        {
            Type = ActivityType.Message,
        };
        act.Properties["unknownString"] = "some string";
        act.Properties["unknownInt"] = 123;
        act.Properties["unknownBool"] = true;
        act.Properties["unknownNull"] = null;
        act.Properties["unknownLong"] = 1L;
        act.Properties["unknownDouble"] = 1.0;

        string json = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json);
        Assert.Contains("\"unknownString\": \"some string\"", json);
        Assert.Contains("\"unknownInt\": 123", json);
        Assert.Contains("\"unknownBool\": true", json);
        Assert.Contains("\"unknownNull\": null", json);
        Assert.Contains("\"unknownLong\": 1", json);
        Assert.Contains("\"unknownDouble\": 1", json);
    }

    [Fact]
    public void Deserialize_Unkown__Fields_In_KnownObjects()
    {
        string json = """
        {
            "type": "message",
            "text": "hello",
            "from": {
                "id": "1",
                "name": "tester",
                "aadObjectId": "123"
            }
        }
        """;
        CoreActivity act = CoreActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("message", act.Type);
        Assert.True(act.Properties.ContainsKey("from"));
        Assert.IsType<JsonElement>(act.Properties["from"]);
        var fromElement = (JsonElement)act.Properties["from"]!;
        Assert.Equal("1", fromElement.GetProperty("id").GetString());
        Assert.Equal("tester", fromElement.GetProperty("name").GetString());
        Assert.Equal("123", fromElement.GetProperty("aadObjectId").GetString());
    }

    [Fact]
    public void Deserialize_Serialize_Unkown__Fields_In_KnownObjects()
    {
        string json = """
        {
            "type": "message",
            "text": "hello",
            "from": {
                "id": "1",
                "name": "tester",
                "aadObjectId": "123"
            }
        }
        """;
        CoreActivity act = CoreActivity.FromJsonString(json);
        string json2 = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json2);
        Assert.Contains("\"text\": \"hello\"", json2);
        Assert.Contains("\"from\":", json2);
        Assert.Contains("\"id\": \"1\"", json2);
        Assert.Contains("\"name\": \"tester\"", json2);
        Assert.Contains("\"aadObjectId\": \"123\"", json2);
    }

    [Fact]
    public void Deserialize_Serialize_Entities()
    {
        string json = """
        {
            "type": "message",
            "text": "hello",
            "entities": [
            {
              "mentioned": {
                "id": "28:0b6fe6d1-fece-44f7-9a48-56465e2d5ab8",
                "name": "ridotest"
              },
              "text": "\u003Cat\u003Eridotest\u003C/at\u003E",
              "type": "mention"
            },
            {
              "locale": "en-US",
              "country": "US",
              "platform": "Web",
              "timezone": "America/Los_Angeles",
              "type": "clientInfo"
            }
          ]
        }
        """;
        CoreActivity act = CoreActivity.FromJsonString(json);
        string json2 = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json2);
        Assert.True(act.Properties.ContainsKey("entities"));
        Assert.IsType<JsonElement>(act.Properties["entities"]);
        var entitiesElement = (JsonElement)act.Properties["entities"]!;
        Assert.Equal(JsonValueKind.Array, entitiesElement.ValueKind);
        Assert.Equal(2, entitiesElement.GetArrayLength());

    }


    [Fact]
    public void Handling_Nulls_from_default_serializer()
    {
        string json = """
        {
            "type": "message",
            "text": null,
            "unknownString": null
        }
        """;
        CoreActivity? act = JsonSerializer.Deserialize<CoreActivity>(json); //without default options
        Assert.NotNull(act);
        Assert.Equal("message", act.Type);
        Assert.Null(act.Properties["text"]);
        Assert.Null(act.Properties["unknownString"]!);

        string json2 = JsonSerializer.Serialize(act); //without default options
        Assert.Contains("\"type\":\"message\"", json2);
        Assert.Contains("\"text\":null", json2);
        Assert.Contains("\"unknownString\":null", json2);
    }

    [Fact]
    public void Serialize_With_Properties_Initialized()
    {
        CoreActivity act = new()
        {
            Type = ActivityType.Message,
            Properties =
            {
                { "customField", "customValue" },
                { "channelData", new ChannelData { Properties = { { "channelCustomField", "channelCustomValue" } } } },
                { "conversation", new Conversation { Properties = { { "conversationCustomField", "conversationCustomValue" } } } },
                { "from", new ConversationAccount { Id = "user1", Properties = { { "fromCustomField", "fromCustomValue" } } } },
                { "recipient", new ConversationAccount { Id = "bot1", Properties = { { "recipientCustomField", "recipientCustomValue" } } } }
            }
        };
        string json = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json);
        Assert.Contains("\"customField\": \"customValue\"", json);
        Assert.Contains("\"channelCustomField\": \"channelCustomValue\"", json);
        Assert.Contains("\"conversationCustomField\": \"conversationCustomValue\"", json);
        Assert.Contains("\"fromCustomField\": \"fromCustomValue\"", json);
        Assert.Contains("\"recipientCustomField\": \"recipientCustomValue\"", json);
    }


    [Fact]
    public async Task DeserializeAsync()
    {
        string json = """
        {
            "type": "message",
            "text": "hello",
            "from": {
                "id": "1",
                "name": "tester",
                "aadObjectId": "123"
            }
        }
        """;
        using MemoryStream ms = new(System.Text.Encoding.UTF8.GetBytes(json));
        CoreActivity? act = await CoreActivity.FromJsonStreamAsync(ms);
        Assert.NotNull(act);
        Assert.Equal("message", act.Type);
        Assert.Equal("hello", act.Properties["text"]?.ToString());
        Assert.True(act.Properties.ContainsKey("from"));
        Assert.IsType<JsonElement>(act.Properties["from"]);
        var fromElement = (JsonElement)act.Properties["from"]!;
        Assert.Equal("1", fromElement.GetProperty("id").GetString());
        Assert.Equal("tester", fromElement.GetProperty("name").GetString());
        Assert.Equal("123", fromElement.GetProperty("aadObjectId").GetString());
    }


    [Fact]
    public async Task DeserializeInvokeWithValueAsync()
    {
        string json = """
        {
            "type": "invoke",
            "value": {
                "key1": "value1",
                "key2": 2
            }
        }
        """;
        using MemoryStream ms = new(System.Text.Encoding.UTF8.GetBytes(json));
        CoreActivity? act = await CoreActivity.FromJsonStreamAsync(ms);
        Assert.NotNull(act);
        Assert.Equal("invoke", act.Type);
        Assert.NotNull(act.Value);
        Assert.NotNull(act.Value["key1"]);
        Assert.Equal("value1", act.Value["key1"]?.GetValue<string>());
        Assert.Equal(2, act.Value["key2"]?.GetValue<int>());
    }

    [Fact]
    public void IsTargeted_DefaultsToNull()
    {
        ConversationAccount account = new();

        Assert.Null(account.IsTargeted);
    }

    [Fact]
    public void IsTargeted_CanBeSetToTrue()
    {
        ConversationAccount account = new()
        {
            IsTargeted = true
        };

        Assert.True(account.IsTargeted);
    }

    [Fact]
    public void IsTargeted_IsSerializedToJson()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties =
            {
                { "recipient", new ConversationAccount { Id = "user-123", IsTargeted = true } }
            }
        };

        string json = activity.ToJson();

        // IsTargeted is serialized in the recipient object
        Assert.Contains("isTargeted", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsTargeted_DeserializedFromJson()
    {
        string json = """
        {
            "type": "message",
            "recipient": {
                "id": "user-123",
                "isTargeted": true
            }
        }
        """;

        CoreActivity activity = CoreActivity.FromJsonString(json);

        Assert.True(activity.Properties.ContainsKey("recipient"));
        Assert.IsType<JsonElement>(activity.Properties["recipient"]);
        var recipientElement = (JsonElement)activity.Properties["recipient"]!;
        Assert.Equal("user-123", recipientElement.GetProperty("id").GetString());
        Assert.True(recipientElement.GetProperty("isTargeted").GetBoolean());
    }
}
