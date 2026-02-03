// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Handlers;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class ConversationUpdateActivityTests
{
    [Fact]
    public void Constructor_Default_SetsConversationUpdateType()
    {
        ConversationUpdateActivity activity = new();
        Assert.Equal(TeamsActivityType.ConversationUpdate, activity.Type);
    }

    [Fact]
    public void AsConversationUpdate_MembersAdded()
    {
        string json = """
        {
            "type": "conversationUpdate",
            "conversation": {
                "id": "19"
            },
            "membersAdded": [
                {
                    "id": "user1",
                    "name": "User One"
                },
                {
                    "id": "bot1",
                    "name": "Bot One"
                }
            ]
        }
        """;
        ConversationUpdateActivity act = ConversationUpdateActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("conversationUpdate", act.Type);
        Assert.NotNull(act.MembersAdded);
        Assert.Equal(2, act.MembersAdded!.Count);
        Assert.Equal("user1", act.MembersAdded[0].Id);
        Assert.Equal("bot1", act.MembersAdded[1].Id);
    }

    [Fact]
    public void AsConversationUpdate_MembersRemoved()
    {
        string json = """
        {
            "type": "conversationUpdate",
            "conversation": {
                "id": "19"
            },
            "membersRemoved": [
                {
                    "id": "user2",
                    "name": "User Two"
                }
            ]
        }
        """;
        ConversationUpdateActivity act = ConversationUpdateActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("conversationUpdate", act.Type);
        Assert.NotNull(act.MembersRemoved);
        Assert.Single(act.MembersRemoved!);
        Assert.Equal("user2", act.MembersRemoved[0].Id);
    }

    [Fact]
    public void AsConversationUpdate_BothMembersAddedAndRemoved()
    {
        string json = """
        {
            "type": "conversationUpdate",
            "conversation": {
                "id": "19"
            },
            "membersAdded": [
                {
                    "id": "newuser",
                    "name": "New User"
                }
            ],
            "membersRemoved": [
                {
                    "id": "olduser",
                    "name": "Old User"
                }
            ]
        }
        """;
        ConversationUpdateActivity act = ConversationUpdateActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("conversationUpdate", act.Type);
        Assert.NotNull(act.MembersAdded);
        Assert.NotNull(act.MembersRemoved);
        Assert.Single(act.MembersAdded!);
        Assert.Single(act.MembersRemoved!);
        Assert.Equal("newuser", act.MembersAdded[0].Id);
        Assert.Equal("olduser", act.MembersRemoved[0].Id);
    }

    [Fact]
    public void SerializeConversationUpdateToJson()
    {
        var activity = new ConversationUpdateActivity
        {
            TopicName = "Test Topic",
            HistoryDisclosed = true
        };

        string json = activity.ToJson();
        Assert.Contains("\"type\": \"conversationUpdate\"", json);
        Assert.Contains("\"topicName\": \"Test Topic\"", json);
        Assert.Contains("\"historyDisclosed\": true", json);
    }

    [Fact]
    public void FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.ConversationUpdate
        };
        coreActivity.Properties["topicName"] = "Converted Topic";

        ConversationUpdateActivity activity = ConversationUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityType.ConversationUpdate, activity.Type);
        Assert.Equal("Converted Topic", activity.TopicName);
    }

    [Fact]
    public void ConversationUpdateActivity_SerializedAsCoreActivity_IncludesProperties()
    {
        ConversationUpdateActivity conversationUpdateActivity = new()
        {
            TopicName = "New Topic",
            HistoryDisclosed = true,
            MembersAdded = new List<TeamsConversationAccount>
            {
                new() { Id = "user1", Name = "User One" }
            },
            Type = TeamsActivityType.ConversationUpdate,
            ServiceUrl = new Uri("https://test.service.url/")
        };

        CoreActivity coreActivity = conversationUpdateActivity;
        string json = coreActivity.ToJson();

        Assert.Contains("\"topicName\"", json);
        Assert.Contains("New Topic", json);
        Assert.Contains("\"historyDisclosed\": true", json);
        Assert.Contains("\"membersAdded\"", json);
        Assert.Contains("user1", json);
        Assert.Contains("\"type\": \"conversationUpdate\"", json);
    }
}
