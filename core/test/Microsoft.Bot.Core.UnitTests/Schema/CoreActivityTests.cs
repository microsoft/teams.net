using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.UnitTests.Schema;

public class CoreCoreActivityTests
{
    [Fact]
    public void Ctor_And_Nulls()
    {
        CoreActivity a1 = new();
        Assert.NotNull(a1);
        Assert.Equal(ActivityTypes.Message, a1.Type);
        Assert.Null(a1.Text);

        CoreActivity a2 = new()
        {
            Type = "mytype"
        };
        Assert.NotNull(a2);
        Assert.Equal("mytype", a2.Type);
        Assert.Null(a2.Text);
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
        Assert.Null(act.Text);

        string json2 = """
        {
            "type": "message"
        }
        """;
        CoreActivity act2 = CoreActivity.FromJsonString(json2);
        Assert.NotNull(act2);
        Assert.Equal("message", act2.Type);
        Assert.Null(act2.Text);

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
        Assert.Equal("hello", act.Text);
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
            Type = ActivityTypes.Message,
            Text = "hello",
        };
        act.Properties["unknownString"] = "some string";
        act.Properties["unknownInt"] = 123;
        act.Properties["unknownBool"] = true;
        act.Properties["unknownNull"] = null;
        act.Properties["unknownLong"] = 1L;
        act.Properties["unknownDouble"] = 1.0;

        string json = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json);
        Assert.Contains("\"text\": \"hello\"", json);
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
        Assert.Equal("hello", act.Text);
        Assert.NotNull(act.From);
        Assert.IsType<ConversationAccount>(act.From);
        Assert.Equal("1", act.From!.Id);
        Assert.Equal("tester", act.From.Name);
        Assert.True(act.From.Properties.ContainsKey("aadObjectId"));
        Assert.Equal("123", act.From.Properties["aadObjectId"]?.ToString());
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
        act.Text = "updated";
        string json2 = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json2);
        Assert.Contains("\"text\": \"updated\"", json2);
        Assert.Contains("\"from\": {", json2);
        Assert.Contains("\"id\": \"1\"", json2);
        Assert.Contains("\"name\": \"tester\"", json2);
        Assert.Contains("\"aadObjectId\": \"123\"", json2);
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
        Assert.Null(act.Text);
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
            Type = ActivityTypes.Message,
            Text = "hello",
            Properties =
            {
                { "customField", "customValue" }
            },
            ChannelData = new()
            {
                Properties =
                {
                    { "channelCustomField", "channelCustomValue" }
                }
            },
            Conversation = new()
            {
                Properties =
                {
                    { "conversationCustomField", "conversationCustomValue" }
                }
            },
            From = new()
            {
                Id = "user1",
                Properties =
                {
                    { "fromCustomField", "fromCustomValue" }
                }
            },
            Recipient = new()
            {
                Id = "bot1",
                Properties =
                {
                    { "recipientCustomField", "recipientCustomValue" }
                }

            }
        };
        string json = act.ToJson();
        Assert.Contains("\"type\": \"message\"", json);
        Assert.Contains("\"text\": \"hello\"", json);
        Assert.Contains("\"customField\": \"customValue\"", json);
        Assert.Contains("\"channelCustomField\": \"channelCustomValue\"", json);
        Assert.Contains("\"conversationCustomField\": \"conversationCustomValue\"", json);
        Assert.Contains("\"fromCustomField\": \"fromCustomValue\"", json);
        Assert.Contains("\"recipientCustomField\": \"recipientCustomValue\"", json);
    }


    [Fact]
    public void CreateReply()
    {
        CoreActivity act = new()
        {
            Type = "myActivityType",
            Text = "hello",
            Id = "CoreActivity1",
            ChannelId = "channel1",
            ServiceUrl = new Uri("http://service.url"),
            From = new ConversationAccount()
            {
                Id = "user1",
                Name = "User One"
            },
            Recipient = new ConversationAccount()
            {
                Id = "bot1",
                Name = "Bot One"
            },
            Conversation = new Conversation()
            {
                Id = "conversation1"
            }
        };
        CoreActivity reply = act.CreateReplyMessageActivity("reply");
        Assert.NotNull(reply);
        Assert.Equal(ActivityTypes.Message, reply.Type);
        Assert.Equal("reply", reply.Text);
        Assert.Equal("channel1", reply.ChannelId);
        Assert.NotNull(reply.ServiceUrl);
        Assert.Equal("http://service.url/", reply.ServiceUrl.ToString());
        Assert.Equal("conversation1", reply.Conversation.Id);
        Assert.Equal("bot1", reply.From.Id);
        Assert.Equal("Bot One", reply.From.Name);
        Assert.Equal("user1", reply.Recipient.Id);
        Assert.Equal("User One", reply.Recipient.Name);
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
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        CoreActivity? act = await CoreActivity.FromJsonStreamAsync(ms);
        Assert.NotNull(act);
        Assert.Equal("message", act.Type);
        Assert.Equal("hello", act.Text);
        Assert.NotNull(act.From);
        Assert.IsType<ConversationAccount>(act.From);
        Assert.Equal("1", act.From.Id);
        Assert.Equal("tester", act.From.Name);
        Assert.True(act.From.Properties.ContainsKey("aadObjectId"));
        Assert.Equal("123", act.From.Properties["aadObjectId"]?.ToString());
    }
}
