using System.Text.Json;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities;

public class ActivityTests
{
    [Fact]
    public void JsonSerialize_Class()
    {
        var activity = new Activity("unknown")
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

        activity.Properties["hello"] = "world";

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Activity.json"
        ), json);

        var newActivity = JsonSerializer.Deserialize<Activity>(json);
        Assert.Equal(newActivity?.ToString(), activity.ToString());
    }

    [Fact]
    public void JsonSerialize_Interface()
    {
        IActivity activity = new Activity("unknown")
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

        activity.Properties["hello"] = "world";

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Activity.json"
        ), json);

        var newActivity = JsonSerializer.Deserialize<IActivity>(json);
        Assert.Equal(newActivity?.ToString(), activity.ToString());
    }
}