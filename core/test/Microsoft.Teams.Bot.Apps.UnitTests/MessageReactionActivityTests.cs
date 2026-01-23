// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class MessageReactionActivityTests
{
    [Fact]
    public void DeserializeMessageReactionFromJson()
    {
        string json = """
        {
            "type": "messageReaction",
            "conversation": {
                "id": "19"
            },
            "reactionsAdded": [
                {
                    "type": "like"
                },
                {
                    "type": "heart"
                }
            ]
        }
        """;
        MessageReactionActivity act = MessageReactionActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("messageReaction", act.Type);
        Assert.NotNull(act.ReactionsAdded);
        Assert.Equal(2, act.ReactionsAdded!.Count);
        Assert.Equal("like", act.ReactionsAdded[0].Type);
        Assert.Equal("heart", act.ReactionsAdded[1].Type);
    }

    [Fact]
    public void DeserializeMessageReactionWithReactionsRemoved()
    {
        string json = """
        {
            "type": "messageReaction",
            "conversation": {
                "id": "19"
            },
            "reactionsRemoved": [
                {
                    "type": "sad"
                }
            ]
        }
        """;
        MessageReactionActivity act = MessageReactionActivity.FromJsonString(json);
        Assert.NotNull(act);

        Assert.NotNull(act.ReactionsRemoved);
        Assert.Single(act.ReactionsRemoved!);
        Assert.Equal("sad", act.ReactionsRemoved[0].Type);
    }

    [Fact]
    public void SerializeMessageReactionToJson()
    {
        var activity = new MessageReactionActivity
        {
            ReactionsAdded = new List<MessageReaction>
            {
                new MessageReaction { Type = ReactionTypes.Like },
                new MessageReaction { Type = ReactionTypes.Heart }
            }
        };

        string json = activity.ToJson();
        Assert.Contains("\"type\": \"messageReaction\"", json);
        Assert.Contains("\"reactionsAdded\"", json);
        Assert.Contains("\"like\"", json);
        Assert.Contains("\"heart\"", json);
    }

    [Fact]
    public void FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = ActivityType.MessageReaction
        };
        coreActivity.Properties["reactionsAdded"] = System.Text.Json.JsonSerializer.SerializeToElement(new[]
        {
            new { type = "like" },
            new { type = "heart" }
        });

        MessageReactionActivity activity = MessageReactionActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(ActivityType.MessageReaction, activity.Type);
        Assert.NotNull(activity.ReactionsAdded);
        Assert.Equal(2, activity.ReactionsAdded!.Count);
    }

    [Fact]
    public void MessageReactionWithUserInfo()
    {
        string json = """
        {
            "type": "messageReaction",
            "conversation": {
                "id": "19"
            },
            "reactionsAdded": [
                {
                    "type": "like",
                    "createdDateTime": "2026-01-22T12:00:00Z",
                    "user": {
                        "id": "user-123",
                        "displayName": "Test User",
                        "userIdentityType": "aadUser"
                    }
                }
            ]
        }
        """;
        MessageReactionActivity activity = MessageReactionActivity.FromJsonString(json);
        Assert.NotNull(activity.ReactionsAdded);
        Assert.Single(activity.ReactionsAdded!);
        Assert.Equal("like", activity.ReactionsAdded[0].Type);
        Assert.NotNull(activity.ReactionsAdded[0].User);
        Assert.Equal("user-123", activity.ReactionsAdded[0].User!.Id);
        Assert.Equal("Test User", activity.ReactionsAdded[0].User!.DisplayName);
        Assert.Equal(UserIdentityTypes.AadUser, activity.ReactionsAdded[0].User!.UserIdentityType);
    }
}
