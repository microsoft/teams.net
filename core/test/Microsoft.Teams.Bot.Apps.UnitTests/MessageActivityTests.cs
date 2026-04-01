// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

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
        //Assert.Equal("This is a summary", messageActivity.Summary);
        Assert.Equal("plain", messageActivity.TextFormat);
        //Assert.Equal(InputHints.AcceptingInput, messageActivity.InputHint);
        //Assert.Equal(ImportanceLevels.High, messageActivity.Importance);
        //Assert.Equal(DeliveryModes.Normal, messageActivity.DeliveryMode);
        Assert.Equal("carousel", messageActivity.AttachmentLayout);
        //Assert.NotNull(messageActivity.Expiration);
    }

    [Fact]
    public void MessageActivity_Serialize_ToJson()
    {
        MessageActivity activity = new("Hello World")
        {
            // Summary = "Test summary",
            TextFormat = TextFormats.Markdown,
            //InputHint = InputHints.ExpectingInput,
            //Importance = ImportanceLevels.Urgent,
            //DeliveryMode = DeliveryModes.Notification
        };

        string json = activity.ToJson();

        Assert.Contains("Hello World", json);
        //Assert.Contains("Test summary", json);
        Assert.Contains("markdown", json);
        //Assert.Contains("expectingInput", json);
        //Assert.Contains("urgent", json);
        //Assert.Contains("notification", json);
    }

    /*
    [Fact]
    public void MessageActivity_WithSpeak_Serialize()
    {
        MessageActivity activity = new("Hello")
        {
            Speak = "<speak>Hello World</speak>"
        };

        string json = activity.ToJson();
        Assert.Contains("\"speak\":", json);
        Assert.Contains("Hello World", json);
    }

    [Fact]
    public void MessageActivity_WithExpiration_Serialize()
    {
        string expirationDate = "2026-12-31T23:59:59Z";
        MessageActivity activity = new("Expiring message")
        {
            Expiration = expirationDate
        };

        string json = activity.ToJson();
        Assert.Contains("2026-12-31T23:59:59Z", json);
    }
    */


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
    }

    [Fact]
    public void MessageActivity_FromCoreActivity_WithMissingProperties_HandlesGracefully()
    {
        CoreActivity coreActivity = new(ActivityType.Message);
        MessageActivity messageActivity = MessageActivity.FromActivity(coreActivity);

        Assert.Null(messageActivity.Text);
        //Assert.Null(messageActivity.Speak);
        //Assert.Null(messageActivity.InputHint);
        //Assert.Null(messageActivity.Summary);
        Assert.Null(messageActivity.TextFormat);
        Assert.Null(messageActivity.AttachmentLayout);
        //Assert.Null(messageActivity.Importance);
        //Assert.Null(messageActivity.DeliveryMode);
        //Assert.Null(messageActivity.Expiration);
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
