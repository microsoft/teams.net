// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class MessageDeleteActivityTests
{
    [Fact]
    public void Constructor_Default_SetsMessageDeleteType()
    {
        MessageDeleteActivity activity = new();
        Assert.Equal(TeamsActivityType.MessageDelete, activity.Type);
    }

    [Fact]
    public void DeserializeMessageDeleteFromJson()
    {
        string json = """
        {
            "type": "messageDelete",
            "conversation": {
                "id": "19"
            },
            "id": "1234567890"
        }
        """;
        MessageDeleteActivity act = MessageDeleteActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("messageDelete", act.Type);

        Assert.Equal("1234567890", act.Id);
    }

    [Fact]
    public void SerializeMessageDeleteToJson()
    {
        var activity = new MessageDeleteActivity
        {
            Id = "msg123"
        };

        string json = activity.ToJson();
        Assert.Contains("\"type\": \"messageDelete\"", json);
        Assert.Contains("\"id\": \"msg123\"", json);
    }

    [Fact]
    public void FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.MessageDelete,
            Id = "deleted-msg-id"
        };

        MessageDeleteActivity messageDelete = MessageDeleteActivity.FromActivity(coreActivity);
        Assert.NotNull(messageDelete);
        Assert.Equal(TeamsActivityType.MessageDelete, messageDelete.Type);
        Assert.Equal("deleted-msg-id", messageDelete.Id);
    }

    [Fact]
    public void FromJsonStringCreatesCorrectType()
    {
        string json = """
        {
            "type": "messageDelete",
            "id": "test-id",
            "conversation": {
                "id": "conv-123"
            }
        }
        """;

        TeamsActivity activity = TeamsActivity.FromJsonString(json);
        Assert.IsType<MessageDeleteActivity>(activity);
        MessageDeleteActivity? mda = activity as MessageDeleteActivity;
        Assert.NotNull(mda);
        Assert.Equal(TeamsActivityType.MessageDelete, mda.Type);
        Assert.Equal("test-id", activity.Id);
    }
}
