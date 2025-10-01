using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities;

public class MessageActivityTests
{
    [Fact]
    public void JsonSerialize()
    {
        var activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Derived()
    {
        MessageActivity activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Derived_Interface()
    {
        Activity activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Mention()
    {
        Account bot = new()
        {
            Id = "2",
            Name = "test-bot",
            Role = Role.Bot
        };

        Activity activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = bot
        }.AddMention(bot);

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var text = File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity_Mention.json",
            Encoding.UTF8
        );

        Assert.Equal(text, json);
    }

    [Fact]
    public void JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity.json");
        var activity = JsonSerializer.Deserialize<MessageActivity>(json);
        var expected = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        Assert.Equivalent(expected, activity);
    }

    [Fact]
    public void JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        Assert.Equivalent(expected, activity);
    }

    [Fact]
    public void JsonSerialize_WebChat_AllowsEmptyConversationType()
    {
        MessageActivity activity = new MessageActivity()
        {
            Id = "1",
            ChannelId = new ChannelId("webchat"),
            From = new Account()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new Api.Conversation()
            {
                Id = "1"
            },
            Recipient = new Account
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };
        string json = JsonSerializer.Serialize(activity, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        string expected = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity_webChat.json");
        Assert.Equal(expected, json);
    }

    [Fact]
    public void JsonDeserialize_WebChat_AllowsEmptyConversationType()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity_webChat.json");
        var activity = JsonSerializer.Deserialize<MessageActivity>(json);
        Assert.NotNull(activity);
        Assert.Equal("1", activity.Id);
        Assert.Equal("webchat", activity.ChannelId);
        Assert.NotNull(activity.From);
        Assert.Equal("1", activity.From.Id);
        Assert.Equal("test", activity.From.Name);
        Assert.NotNull(activity.From.Role);
        Assert.Equal(Role.User, activity.From.Role.Value);
        Assert.NotNull(activity.Conversation);
        Assert.Equal("1", activity.Conversation.Id);
        Assert.Null(activity.Conversation.Type);
        Assert.NotNull(activity.Recipient);
        Assert.Equal("2", activity.Recipient.Id);
        Assert.Equal("test-bot", activity.Recipient.Name);
        Assert.NotNull(activity.Recipient.Role);
        Assert.Equal(Role.Bot, activity.Recipient.Role.Value);
    }
}