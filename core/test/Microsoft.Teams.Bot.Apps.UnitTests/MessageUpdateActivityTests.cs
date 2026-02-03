// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Handlers;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class MessageUpdateActivityTests
{
    [Fact]
    public void Constructor_Default_SetsMessageUpdateType()
    {
        MessageUpdateActivity activity = new();
        Assert.Equal(TeamsActivityType.MessageUpdate, activity.Type);
    }

    [Fact]
    public void Constructor_WithText_SetsTextAndMessageUpdateType()
    {
        MessageUpdateActivity activity = new("Updated text");
        Assert.Equal(TeamsActivityType.MessageUpdate, activity.Type);
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
            Type = TeamsActivityType.MessageUpdate
        };
        coreActivity.Properties["text"] = "Test message";

        MessageUpdateActivity messageUpdate = MessageUpdateActivity.FromActivity(coreActivity);
        Assert.NotNull(messageUpdate);
        Assert.Equal(TeamsActivityType.MessageUpdate, messageUpdate.Type);
        Assert.Equal("Test message", messageUpdate.Text);
    }

    [Fact]
    public void MessageUpdateActivity_SerializedAsCoreActivity_IncludesText()
    {
        MessageUpdateActivity messageUpdateActivity = new("Message update text")
        {
            Type = TeamsActivityType.MessageUpdate,
            ServiceUrl = new Uri("https://test.service.url/"),
            Speak = "Message update spoken"
        };

        CoreActivity coreActivity = messageUpdateActivity;
        string json = coreActivity.ToJson();

        Assert.Contains("Message update text", json);
        Assert.Contains("\"text\"", json);
        Assert.Contains("Message update spoken", json);
        Assert.Contains("\"speak\"", json);
        Assert.Contains("messageUpdate", json);
    }
}
