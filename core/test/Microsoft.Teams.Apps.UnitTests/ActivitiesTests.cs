// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Tests for simple activity types.
/// </summary>
public class ActivitiesTests
{
    [Fact]
    public void MessageReaction_FromActivityConvertsCorrectly()
    {
        CoreActivity coreActivity = new()
        {
            Type = TeamsActivityTypes.MessageReaction
        };
        coreActivity.Properties["reactionsAdded"] = System.Text.Json.JsonSerializer.SerializeToElement(new[]
        {
            new { type = "like" },
            new { type = "heart" }
        });

        MessageReactionActivity activity = MessageReactionActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityTypes.MessageReaction, activity.Type);
        Assert.NotNull(activity.ReactionsAdded);
        Assert.Equal(2, activity.ReactionsAdded!.Count);
    }

    [Fact]
    public void MessageDelete_FromActivityConvertsCorrectly()
    {
        CoreActivity coreActivity = new()
        {
            Type = TeamsActivityTypes.MessageDelete,
            Id = "deleted-msg-id"
        };

        MessageDeleteActivity messageDelete = MessageDeleteActivity.FromActivity(coreActivity);
        Assert.NotNull(messageDelete);
        Assert.Equal(TeamsActivityTypes.MessageDelete, messageDelete.Type);
        Assert.Equal("deleted-msg-id", messageDelete.Id);
    }

    [Fact]
    public void MessageUpdate_FromActivityConvertsCorrectly()
    {
        CoreActivity coreActivity = new()
        {
            Type = TeamsActivityTypes.MessageUpdate
        };
        coreActivity.Properties["text"] = "Test message";

        MessageUpdateActivity messageUpdate = MessageUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(messageUpdate);
        Assert.Equal(TeamsActivityTypes.MessageUpdate, messageUpdate.Type);
        Assert.Equal("Test message", messageUpdate.Text);
    }

    [Fact]
    public void ConversationUpdate_FromActivityConvertsCorrectly()
    {
        CoreActivity coreActivity = new()
        {
            Type = TeamsActivityTypes.ConversationUpdate
        };
        //coreActivity.Properties["topicName"] = "Converted Topic";

        ConversationUpdateActivity activity = ConversationUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityTypes.ConversationUpdate, activity.Type);
        //Assert.Equal("Converted Topic", activity.TopicName);
    }

    [Fact]
    public void InstallUpdate_FromActivityConvertsCorrectly()
    {
        CoreActivity coreActivity = new()
        {
            Type = TeamsActivityTypes.InstallationUpdate
        };
        coreActivity.Properties["action"] = "remove";

        InstallUpdateActivity activity = InstallUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityTypes.InstallationUpdate, activity.Type);
        Assert.Equal("remove", activity.Action);
    }
}
