// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class MessageActivityTests
{
    [Fact]
    public void Constructor_Default_SetsMessageType()
    {
        MessageActivity activity = new();
        Assert.Equal(TeamsActivityType.Message, activity.Type);
    }

    [Fact]
    public void Constructor_WithText_SetsTextAndMessageType()
    {
        MessageActivity activity = new("Hello World");
        Assert.Equal(TeamsActivityType.Message, activity.Type);
        Assert.Equal("Hello World", activity.Text);
    }

    [Fact]
    public void MessageActivity_FromCoreActivity_MapsAllProperties()
    {
        CoreActivity coreActivity = CoreActivity.FromJsonString(jsonMessageWithAllProps);
        MessageActivity messageActivity = MessageActivity.FromActivity(coreActivity);

        Assert.Equal("Hello World", messageActivity.Text);
        Assert.Equal("plain", messageActivity.TextFormat);
        Assert.Equal("carousel", messageActivity.AttachmentLayout);
        Assert.NotNull(messageActivity.From);
        Assert.Equal("user-123", messageActivity.From.Id);
        Assert.Equal("Test User", messageActivity.From.Name);
        Assert.NotNull(messageActivity.Recipient);
        Assert.Equal("bot-123", messageActivity.Recipient.Id);
        Assert.Equal("Test Bot", messageActivity.Recipient.Name);
    }

    [Fact]
    public void MessageActivity_Serialize_ToJson()
    {
        MessageActivity activity = new("Hello World")
        {
            TextFormat = TextFormats.Markdown,
        };

        string json = activity.ToJson();

        Assert.Contains("Hello World", json);
        Assert.Contains("markdown", json);
    }

    [Fact]
    public void MessageActivity_Constants_TextFormats()
    {
        MessageActivity activity = new("Test")
        {
            TextFormat = TextFormats.Plain
        };
        Assert.Equal("plain", activity.TextFormat);

        activity.TextFormat = TextFormats.Markdown;
        Assert.Equal("markdown", activity.TextFormat);

        activity.TextFormat = TextFormats.Xml;
        Assert.Equal("xml", activity.TextFormat);

        activity.TextFormat = TextFormats.ExtendedMarkdown;
        Assert.Equal("extendedmarkdown", activity.TextFormat);
    }

    [Fact]
    public void MessageActivity_FromCoreActivity_WithMissingProperties_HandlesGracefully()
    {
        CoreActivity coreActivity = new(ActivityType.Message);
        MessageActivity messageActivity = MessageActivity.FromActivity(coreActivity);

        Assert.Null(messageActivity.Text);
        Assert.Null(messageActivity.TextFormat);
        Assert.Null(messageActivity.AttachmentLayout);
    }

    [Fact]
    public void MessageActivity_SerializedAsCoreActivity_IncludesText()
    {
        MessageActivity messageActivity = new("Hello World")
        {
            Type = ActivityType.Message,
            ServiceUrl = new Uri("https://test.service.url/")
        };

        CoreActivity coreActivity = messageActivity;
        string json = coreActivity.ToJson();

        Assert.Contains("Hello World", json);
        Assert.Contains("\"text\"", json);
    }

    private const string jsonMessageWithAllProps = """
        {
          "type": "message",
          "channelId": "msteams",
          "text": "Hello World",
          "speak": "<speak>Hello World</speak>",
          "inputHint": "acceptingInput",
          "summary": "This is a summary",
          "textFormat": "plain",
          "attachmentLayout": "carousel",
          "importance": "high",
          "deliveryMode": "normal",
          "expiration": "2026-12-31T23:59:59Z",
          "id": "1234567890",
          "timestamp": "2026-01-21T12:00:00Z",
          "serviceUrl": "https://smba.trafficmanager.net/amer/",
          "from": {
            "id": "user-123",
            "name": "Test User"
          },
          "conversation": {
            "id": "conversation-123"
          },
          "recipient": {
            "id": "bot-123",
            "name": "Test Bot"
          }
        }
        """;
}
