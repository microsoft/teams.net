// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class SuggestedActionsTests
{
    [Fact]
    public void CardActionTypes_Constants_HaveExpectedValues()
    {
        Assert.Equal("openUrl", CardActionTypes.OpenUrl);
        Assert.Equal("imBack", CardActionTypes.IMBack);
        Assert.Equal("postBack", CardActionTypes.PostBack);
        Assert.Equal("playAudio", CardActionTypes.PlayAudio);
        Assert.Equal("playVideo", CardActionTypes.PlayVideo);
        Assert.Equal("showImage", CardActionTypes.ShowImage);
        Assert.Equal("downloadFile", CardActionTypes.DownloadFile);
        Assert.Equal("signin", CardActionTypes.SignIn);
        Assert.Equal("call", CardActionTypes.Call);
    }

    [Fact]
    public void CardAction_DefaultConstructor_AllPropertiesNull()
    {
        var action = new CardAction();

        Assert.Null(action.Type);
        Assert.Null(action.Title);
        Assert.Null(action.Image);
        Assert.Null(action.Text);
        Assert.Null(action.DisplayText);
        Assert.Null(action.Value);
        Assert.Null(action.ChannelData);
        Assert.Null(action.ImageAltText);
    }

    [Fact]
    public void CardAction_ConvenienceConstructor_SetsTypeAndTitle()
    {
        var action = new CardAction(CardActionTypes.IMBack, "Say Hello");

        Assert.Equal(CardActionTypes.IMBack, action.Type);
        Assert.Equal("Say Hello", action.Title);
    }

    [Fact]
    public void SuggestedActions_DefaultConstructor_EmptyCollections()
    {
        var suggestedActions = new SuggestedActions();

        Assert.NotNull(suggestedActions.To);
        Assert.Empty(suggestedActions.To);
        Assert.NotNull(suggestedActions.Actions);
        Assert.Empty(suggestedActions.Actions);
    }

    [Fact]
    public void SuggestedActions_AddRecipients_AddsToList()
    {
        var suggestedActions = new SuggestedActions();

        suggestedActions.AddRecipients("user1", "user2");

        Assert.Equal(2, suggestedActions.To.Count);
        Assert.Contains("user1", suggestedActions.To);
        Assert.Contains("user2", suggestedActions.To);
    }

    [Fact]
    public void SuggestedActions_AddAction_AddsToList()
    {
        var suggestedActions = new SuggestedActions();
        var action = new CardAction(CardActionTypes.IMBack, "Click me");

        suggestedActions.AddAction(action);

        Assert.Single(suggestedActions.Actions);
        Assert.Equal("Click me", suggestedActions.Actions[0].Title);
    }

    [Fact]
    public void SuggestedActions_AddActions_AddsMultiple()
    {
        var suggestedActions = new SuggestedActions();

        suggestedActions.AddActions(
            new CardAction(CardActionTypes.IMBack, "Option 1"),
            new CardAction(CardActionTypes.IMBack, "Option 2"),
            new CardAction(CardActionTypes.PostBack, "Option 3")
        );

        Assert.Equal(3, suggestedActions.Actions.Count);
    }

    [Fact]
    public void SuggestedActions_FluentChaining_ReturnsSameInstance()
    {
        var suggestedActions = new SuggestedActions();
        var action = new CardAction(CardActionTypes.IMBack, "Test");

        var result1 = suggestedActions.AddRecipients("user1");
        var result2 = suggestedActions.AddAction(action);
        var result3 = suggestedActions.AddActions(action);

        Assert.Same(suggestedActions, result1);
        Assert.Same(suggestedActions, result2);
        Assert.Same(suggestedActions, result3);
    }

    [Fact]
    public void MessageActivity_SuggestedActions_Serialize()
    {
        var activity = new MessageActivity("Choose an option")
        {
            SuggestedActions = new SuggestedActions()
        };
        activity.SuggestedActions.AddRecipients("user1");
        activity.SuggestedActions.AddAction(new CardAction(CardActionTypes.IMBack, "Option 1") { Value = "opt1" });

        string json = activity.ToJson();

        Assert.Contains("\"suggestedActions\"", json);
        Assert.Contains("\"to\"", json);
        Assert.Contains("\"actions\"", json);
        Assert.Contains("\"imBack\"", json);
        Assert.Contains("\"Option 1\"", json);
        Assert.Contains("\"opt1\"", json);
        Assert.Contains("user1", json);
    }

    [Fact]
    public void MessageActivity_FromCoreActivity_DeserializesSuggestedActions()
    {
        string json = """
        {
          "type": "message",
          "text": "Choose an option",
          "suggestedActions": {
            "to": ["user1", "user2"],
            "actions": [
              {
                "type": "imBack",
                "title": "Option 1",
                "value": "option1"
              },
              {
                "type": "postBack",
                "title": "Option 2",
                "value": "option2"
              }
            ]
          }
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        MessageActivity activity = MessageActivity.FromActivity(coreActivity);

        Assert.NotNull(activity.SuggestedActions);
        Assert.Equal(2, activity.SuggestedActions.To.Count);
        Assert.Contains("user1", activity.SuggestedActions.To);
        Assert.Contains("user2", activity.SuggestedActions.To);
        Assert.Equal(2, activity.SuggestedActions.Actions.Count);
        Assert.Equal("imBack", activity.SuggestedActions.Actions[0].Type);
        Assert.Equal("Option 1", activity.SuggestedActions.Actions[0].Title);
        Assert.Equal("postBack", activity.SuggestedActions.Actions[1].Type);
        Assert.Equal("Option 2", activity.SuggestedActions.Actions[1].Title);
    }

    [Fact]
    public void MessageActivity_WithoutSuggestedActions_PropertyIsNull()
    {
        string json = """
        {
          "type": "message",
          "text": "No suggestions here"
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        MessageActivity activity = MessageActivity.FromActivity(coreActivity);

        Assert.Null(activity.SuggestedActions);
    }

    [Fact]
    public void MessageActivity_WithSuggestedActions_ExtensionSetsProperty()
    {
        var activity = new MessageActivity("Pick one");
        var suggestedActions = new SuggestedActions();
        suggestedActions.AddAction(new CardAction(CardActionTypes.IMBack, "Go"));

        activity.WithSuggestedActions(suggestedActions);

        Assert.NotNull(activity.SuggestedActions);
        Assert.Same(suggestedActions, activity.SuggestedActions);
        Assert.Single(activity.SuggestedActions.Actions);
    }

    [Fact]
    public void MessageActivity_AddSuggestedActions_ExtensionCreatesAndAdds()
    {
        var activity = new MessageActivity("Pick one");

        activity.AddSuggestedActions(
            new CardAction(CardActionTypes.IMBack, "Option A"),
            new CardAction(CardActionTypes.IMBack, "Option B")
        );

        Assert.NotNull(activity.SuggestedActions);
        Assert.Equal(2, activity.SuggestedActions.Actions.Count);
        Assert.Equal("Option A", activity.SuggestedActions.Actions[0].Title);
        Assert.Equal("Option B", activity.SuggestedActions.Actions[1].Title);
    }

    [Fact]
    public void MessageActivity_AddSuggestedActions_WithRecipients()
    {
        var activity = new MessageActivity("Pick one");

        activity.AddSuggestedActions(
            ["user1", "user2"],
            new CardAction(CardActionTypes.IMBack, "Option A")
        );

        Assert.NotNull(activity.SuggestedActions);
        Assert.Equal(2, activity.SuggestedActions.To.Count);
        Assert.Contains("user1", activity.SuggestedActions.To);
        Assert.Single(activity.SuggestedActions.Actions);
    }

    [Fact]
    public void MessageActivity_SuggestedActions_RoundTrip()
    {
        var activity = new MessageActivity("Choose");
        activity.SuggestedActions = new SuggestedActions();
        activity.SuggestedActions.AddRecipients("user1");
        activity.SuggestedActions.AddActions(
            new CardAction(CardActionTypes.OpenUrl, "Open") { Value = "https://example.com" },
            new CardAction(CardActionTypes.IMBack, "Say Hi") { Value = "hi" }
        );

        string json = activity.ToJson();

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        MessageActivity roundTripped = MessageActivity.FromActivity(coreActivity);

        Assert.NotNull(roundTripped.SuggestedActions);
        Assert.Single(roundTripped.SuggestedActions.To);
        Assert.Equal("user1", roundTripped.SuggestedActions.To[0]);
        Assert.Equal(2, roundTripped.SuggestedActions.Actions.Count);
        Assert.Equal("openUrl", roundTripped.SuggestedActions.Actions[0].Type);
        Assert.Equal("Open", roundTripped.SuggestedActions.Actions[0].Title);
        Assert.Equal("imBack", roundTripped.SuggestedActions.Actions[1].Type);
        Assert.Equal("Say Hi", roundTripped.SuggestedActions.Actions[1].Title);
    }
}
