// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class MessageActivityInputTests
{
    [Fact]
    public void Constructor_DefaultConstructor_CreatesNewActivity()
    {
        MessageActivityInput activity = new MessageActivityInput();
        Assert.NotNull(activity);
        Assert.Equal(TeamsActivityTypes.Message, activity.Type);
    }

    [Fact]
    public void WithText_SetsTextContent()
    {
        MessageActivityInput activity = new MessageActivityInput().WithText("Hello, World!");
        Assert.Equal("Hello, World!", activity.Text);
    }

    [Fact]
    public void WithAttachments_SetsAttachmentsCollection()
    {
        MessageActivityInput activity = new MessageActivityInput().WithAttachments([
            new TeamsAttachment { ContentType = new AttachmentContentType("application/json"), Name = "test-attachment" }
        ]);

        Assert.Single(activity.Attachments!);
        Assert.Equal("application/json", activity.Attachments![0].ContentType);
    }

    [Fact]
    public void AddAdaptiveCardAttachment_AddsAdaptiveCard()
    {
        object adaptiveCard = new { type = "AdaptiveCard", version = "1.2" };

        MessageActivityInput activity = new MessageActivityInput().AddAdaptiveCardAttachment(adaptiveCard);

        Assert.Single(activity.Attachments!);
        Assert.Equal("application/vnd.microsoft.card.adaptive", activity.Attachments![0].ContentType);
        Assert.Same(adaptiveCard, activity.Attachments![0].Content);
    }

    [Fact]
    public void AddMention_WithTeamsAccount_AddsMentionAndText()
    {
        TeamsChannelAccount account = new TeamsChannelAccount()
        {
            Id = "user-123",
            Name = "John Doe"
        };

        MessageActivityInput activity = new MessageActivityInput().WithText("said hello").AddMention(account);

        Assert.Equal("<at>John Doe</at> said hello", activity.Text);
        Assert.Single(activity.Entities!);
    }

    [Fact]
    public void AddFeedback_WithMode_SetsFeedbackLoop()
    {
        MessageActivityInput activity = new MessageActivityInput().AddFeedback(FeedbackTypes.Custom);
        Assert.Equal(FeedbackTypes.Custom, activity.ChannelData!.FeedbackLoop!.Type);
    }

    [Fact]
    public void FluentApi_CompleteActivity_BuildsCorrectly()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("Test message")
            .AddEntity(new ClientInfoEntity { Locale = "en-US" })
            .AddAttachment(new TeamsAttachment { ContentType = new AttachmentContentType("text/html") })
            .AddMention(new TeamsChannelAccount { Id = "user-1", Name = "User" });

        Assert.Equal("<at>User</at> Test message", activity.Text);
        Assert.Equal(2, activity.Entities!.Count);
        Assert.Single(activity.Attachments!);
    }

    [Fact]
    public void ToJson_SerializesExpectedShape()
    {
        MessageActivityInput activity = new MessageActivityInput()
            .WithText("Choose")
            .WithSuggestedActions(new SuggestedActions
            {
                To = ["user-1"],
                Actions = [new SuggestedAction(ActionTypes.IMBack, "Option 1")]
            });

        string json = activity.ToJson();

        Assert.Contains("\"suggestedActions\"", json);
    }
}
