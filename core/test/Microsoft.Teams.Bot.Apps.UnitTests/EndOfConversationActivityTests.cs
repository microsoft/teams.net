// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class EndOfConversationActivityTests
{
    [Fact]
    public void Constructor_Default_SetsEndOfConversationType()
    {
        EndOfConversationActivity activity = new();
        Assert.Equal(TeamsActivityType.EndOfConversation, activity.Type);
    }

    [Fact]
    public void DeserializeEndOfConversationFromJson()
    {
        string json = """
        {
            "type": "endOfConversation",
            "conversation": {
                "id": "19"
            },
            "code": "completedSuccessfully",
            "text": "Conversation ended"
        }
        """;
        EndOfConversationActivity act = EndOfConversationActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("endOfConversation", act.Type);
        Assert.Equal("completedSuccessfully", act.Code);
        Assert.Equal("Conversation ended", act.Text);
    }

    [Fact]
    public void SerializeEndOfConversationToJson()
    {
        var activity = new EndOfConversationActivity
        {
            Code = EndOfConversationCodes.UserCancelled,
            Text = "User cancelled"
        };

        string json = activity.ToJson();
        Assert.Contains("\"type\": \"endOfConversation\"", json);
        Assert.Contains("\"code\": \"userCancelled\"", json);
        Assert.Contains("\"text\": \"User cancelled\"", json);
    }

    [Fact]
    public void FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.EndOfConversation
        };
        coreActivity.Properties["code"] = "botTimedOut";
        coreActivity.Properties["text"] = "Bot timeout";

        EndOfConversationActivity activity = EndOfConversationActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityType.EndOfConversation, activity.Type);
        Assert.Equal("botTimedOut", activity.Code);
        Assert.Equal("Bot timeout", activity.Text);
    }

    [Fact]
    public void EndOfConversationActivity_SerializedAsCoreActivity_IncludesProperties()
    {
        EndOfConversationActivity endOfConversationActivity = new()
        {
            Code = EndOfConversationCodes.CompletedSuccessfully,
            Text = "All done",
            Type = TeamsActivityType.EndOfConversation,
            ServiceUrl = new Uri("https://test.service.url/")
        };

        CoreActivity coreActivity = endOfConversationActivity;
        string json = coreActivity.ToJson();

        Assert.Contains("\"code\"", json);
        Assert.Contains("completedSuccessfully", json);
        Assert.Contains("\"text\"", json);
        Assert.Contains("All done", json);
        Assert.Contains("\"type\": \"endOfConversation\"", json);
    }
}
