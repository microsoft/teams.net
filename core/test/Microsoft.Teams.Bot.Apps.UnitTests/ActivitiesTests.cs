// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;
using Microsoft.Teams.Bot.Apps.Schema.InstallActivities;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

/// <summary>
/// Tests for simple activity types.
/// </summary>
public class ActivitiesTests
{
    #region MessageReactionActivity Tests

    [Fact]
    public void MessageReaction_FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.MessageReaction
        };
        coreActivity.Properties["reactionsAdded"] = System.Text.Json.JsonSerializer.SerializeToElement(new[]
        {
            new { type = "like" },
            new { type = "heart" }
        });

        MessageReactionActivity activity = MessageReactionActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityType.MessageReaction, activity.Type);
        Assert.NotNull(activity.ReactionsAdded);
        Assert.Equal(2, activity.ReactionsAdded!.Count);
    }

    #endregion

    #region MessageDeleteActivity Tests

    [Fact]
    public void MessageDelete_Constructor_Default_SetsMessageDeleteType()
    {
        MessageDeleteActivity activity = new();
        Assert.Equal(TeamsActivityType.MessageDelete, activity.Type);
    }

    [Fact]
    public void MessageDelete_FromActivityConvertsCorrectly()
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

    #endregion

    #region MessageUpdateActivity Tests

    [Fact]
    public void MessageUpdate_Constructor_Default_SetsMessageUpdateType()
    {
        MessageUpdateActivity activity = new();
        Assert.Equal(TeamsActivityType.MessageUpdate, activity.Type);
    }

    [Fact]
    public void MessageUpdate_Constructor_WithText_SetsTextAndMessageUpdateType()
    {
        MessageUpdateActivity activity = new("Updated text");
        Assert.Equal(TeamsActivityType.MessageUpdate, activity.Type);
        Assert.Equal("Updated text", activity.Text);
    }

    [Fact]
    public void MessageUpdate_SerializeToJson()
    {
        var activity = new MessageUpdateActivity
        {
            Text = "Updated message",
            Speak = "Updated message spoken"
        };

        string json = activity.ToJson();
        Assert.Contains("\"type\": \"messageUpdate\"", json);
        Assert.Contains("\"text\": \"Updated message\"", json);
        Assert.Contains("\"speak\": \"Updated message spoken\"", json);
    }

    [Fact]
    public void MessageUpdate_InheritsFromMessageActivity()
    {
        var activity = new MessageUpdateActivity
        {
            Text = "Updated",
            InputHint = InputHints.AcceptingInput,
            TextFormat = TextFormats.Markdown
        };

        Assert.Equal("Updated", activity.Text);
        Assert.Equal(InputHints.AcceptingInput, activity.InputHint);
        Assert.Equal(TextFormats.Markdown, activity.TextFormat);
    }

    [Fact]
    public void MessageUpdate_FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.MessageUpdate
        };
        coreActivity.Properties["text"] = "Test message";

        MessageUpdateActivity messageUpdate = MessageUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(messageUpdate);
        Assert.Equal(TeamsActivityType.MessageUpdate, messageUpdate.Type);
        Assert.Equal("Test message", messageUpdate.Text);
    }

    #endregion

    #region ConversationUpdateActivity Tests

    [Fact]
    public void ConversationUpdate_Constructor_Default_SetsConversationUpdateType()
    {
        ConversationUpdateActivity activity = new();
        Assert.Equal(TeamsActivityType.ConversationUpdate, activity.Type);
    }

    [Fact]
    public void ConversationUpdate_FromActivityConvertsCorrectly()
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

    #endregion

    #region EndOfConversationActivity Tests

    [Fact]
    public void EndOfConversation_Constructor_Default_SetsEndOfConversationType()
    {
        EndOfConversationActivity activity = new();
        Assert.Equal(TeamsActivityType.EndOfConversation, activity.Type);
    }

    [Fact]
    public void EndOfConversation_FromActivityConvertsCorrectly()
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

    #endregion

    #region InstallUpdateActivity Tests

    [Fact]
    public void InstallUpdate_Constructor_Default_SetsInstallationUpdateType()
    {
        InstallUpdateActivity activity = new();
        Assert.Equal(TeamsActivityType.InstallationUpdate, activity.Type);
    }

    [Fact]
    public void InstallUpdate_FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.InstallationUpdate
        };
        coreActivity.Properties["action"] = "remove";

        InstallUpdateActivity activity = InstallUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityType.InstallationUpdate, activity.Type);
        Assert.Equal("remove", activity.Action);
    }

    #endregion
}
