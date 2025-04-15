using System.Text.Json;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities;

public class TypingActivityTests
{
    [Fact]
    public void JsonSerialize()
    {
        var activity = new TypingActivity()
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
            @"../../../Json/Activity/TypingActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Derived()
    {
        Activity activity = new TypingActivity()
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
            @"../../../Json/Activity/TypingActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Derived_Interface()
    {
        Activity activity = new TypingActivity()
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
            @"../../../Json/Activity/TypingActivity.json"
        ), json);
    }

    [Fact]
    public void JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/TypingActivity.json");
        var activity = JsonSerializer.Deserialize<TypingActivity>(json);
        var expected = new TypingActivity()
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
        var json = File.ReadAllText(@"../../../Json/Activity/TypingActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = new TypingActivity()
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
}