// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class MessageUpdateActivityTests
{
    [Fact]
    public void Constructor_Default_SetsMessageUpdateType()
    {
        MessageUpdateActivity activity = new();
        Assert.Equal(ActivityType.MessageUpdate, activity.Type);
    }

    [Fact]
    public void Constructor_WithText_SetsTextAndMessageUpdateType()
    {
        MessageUpdateActivity activity = new("Updated text");
        Assert.Equal(ActivityType.MessageUpdate, activity.Type);
        Assert.Equal("Updated text", activity.Text);
    }

    [Fact]
    public void DeserializeMessageUpdateFromJson()
    {
        string json = """
        {
            "type": "messageUpdate",
            "text": "Updated message text",
            "conversation": {
                "id": "19"
            }
        }
        """;
        MessageUpdateActivity act = MessageUpdateActivity.FromJsonString(json);
        Assert.NotNull(act);
        Assert.Equal("messageUpdate", act.Type);

        Assert.Equal("Updated message text", act.Text);
    }

    [Fact]
    public void SerializeMessageUpdateToJson()
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
    public void MessageUpdateInheritsFromMessageActivity()
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
    public void FromActivityConvertsCorrectly()
    {
        var coreActivity = new CoreActivity
        {
            Type = ActivityType.MessageUpdate
        };
        coreActivity.Properties["text"] = "Test message";

        MessageUpdateActivity messageUpdate = MessageUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(messageUpdate);
        Assert.Equal(ActivityType.MessageUpdate, messageUpdate.Type);
        Assert.Equal("Test message", messageUpdate.Text);
    }

    [Fact]
    public void FromJsonStringCreatesCorrectType()
    {
        string json = """
        {
            "type": "messageUpdate",
            "text": "Updated content",
            "textFormat": "markdown",
            "conversation": {
                "id": "conv-123"
            }
        }
        """;

        MessageUpdateActivity activity = MessageUpdateActivity.FromJsonString(json);
        Assert.NotNull(activity);
        Assert.Equal(ActivityType.MessageUpdate, activity.Type);
        Assert.Equal("Updated content", activity.Text);
        Assert.Equal("markdown", activity.TextFormat);
    }
}
