// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable ExperimentalTeamsSuggestedAction

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class SuggestedActionsTests
{
    [Fact]
    public void ActionTypes_Constants_HaveExpectedValues()
    {
        Assert.Equal("openUrl", ActionTypes.OpenUrl);
        Assert.Equal("imBack", ActionTypes.IMBack);
        Assert.Equal("postBack", ActionTypes.PostBack);
        Assert.Equal("playAudio", ActionTypes.PlayAudio);
        Assert.Equal("playVideo", ActionTypes.PlayVideo);
        Assert.Equal("showImage", ActionTypes.ShowImage);
        Assert.Equal("downloadFile", ActionTypes.DownloadFile);
        Assert.Equal("signin", ActionTypes.SignIn);
        Assert.Equal("call", ActionTypes.Call);
        Assert.Equal("Action.Submit", ActionTypes.Submit);
    }

    [Fact]
    public void SuggestedAction_DefaultConstructor_AllPropertiesNull()
    {
        SuggestedAction action = new();

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
    public void SuggestedAction_ConvenienceConstructor_SetsTypeAndTitle()
    {
        SuggestedAction action = new(ActionTypes.IMBack, "Say Hello");

        Assert.Equal(ActionTypes.IMBack, action.Type);
        Assert.Equal("Say Hello", action.Title);
    }

    [Fact]
    public void SuggestedActions_DefaultConstructor_EmptyCollections()
    {
        SuggestedActions suggestedActions = new();

        Assert.NotNull(suggestedActions.To);
        Assert.Empty(suggestedActions.To);
        Assert.NotNull(suggestedActions.Actions);
        Assert.Empty(suggestedActions.Actions);
    }

    [Fact]
    public void SuggestedActions_AddRecipients_AddsToList()
    {
        SuggestedActions suggestedActions = new();

        suggestedActions.AddRecipients("user1", "user2");

        Assert.Equal(2, suggestedActions.To.Count);
        Assert.Contains("user1", suggestedActions.To);
        Assert.Contains("user2", suggestedActions.To);
    }

    [Fact]
    public void SuggestedActions_AddAction_AddsToList()
    {
        SuggestedActions suggestedActions = new();
        SuggestedAction action = new(ActionTypes.IMBack, "Click me");

        suggestedActions.AddAction(action);

        Assert.Single(suggestedActions.Actions);
        Assert.Equal("Click me", suggestedActions.Actions[0].Title);
    }

    [Fact]
    public void SuggestedActions_AddActions_AddsMultiple()
    {
        SuggestedActions suggestedActions = new();

        suggestedActions.AddActions(
            new SuggestedAction(ActionTypes.IMBack, "Option 1"),
            new SuggestedAction(ActionTypes.IMBack, "Option 2"),
            new SuggestedAction(ActionTypes.PostBack, "Option 3")
        );

        Assert.Equal(3, suggestedActions.Actions.Count);
    }

    [Fact]
    public void SuggestedActions_FluentChaining_ReturnsSameInstance()
    {
        SuggestedActions suggestedActions = new();
        SuggestedAction action = new(ActionTypes.IMBack, "Test");

        SuggestedActions result1 = suggestedActions.AddRecipients("user1");
        SuggestedActions result2 = suggestedActions.AddAction(action);
        SuggestedActions result3 = suggestedActions.AddActions(action);

        Assert.Same(suggestedActions, result1);
        Assert.Same(suggestedActions, result2);
        Assert.Same(suggestedActions, result3);
    }

    [Fact]
    public void MessageActivity_SuggestedActions_Serialize()
    {
        MessageActivity activity = new("Choose an option")
        {
            SuggestedActions = new SuggestedActions()
        };
        activity.SuggestedActions.AddRecipients("user1");
        activity.SuggestedActions.AddAction(new SuggestedAction(ActionTypes.IMBack, "Option 1", "opt1"));

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
        Assert.Equal("imBack", activity.SuggestedActions.Actions[0].Type!.ToString());
        Assert.Equal("Option 1", activity.SuggestedActions.Actions[0].Title);
        Assert.Equal("postBack", activity.SuggestedActions.Actions[1].Type!.ToString());
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
    public void MessageActivity_WithSuggestedActions_SetsProperty()
    {
        SuggestedActions suggestedActions = new();

        MessageActivityInput activity = new MessageActivityInput()
            .WithText("Choose an option")
            .WithSuggestedActions(suggestedActions)
            ;

        Assert.NotNull(activity.SuggestedActions);
        Assert.Same(suggestedActions, activity.SuggestedActions);
        Assert.Empty(activity.SuggestedActions.Actions);
    }



    [Fact]
    public void MessageActivity_WithSuggestedActions()
    {
        SuggestedActions suggestedActions = new SuggestedActions()
            .AddAction(new SuggestedAction(ActionTypes.IMBack, "Option 1", "opt1"));

        MessageActivityInput activity = new MessageActivityInput()
            .WithText("Choose an option")
            .WithSuggestedActions(suggestedActions)
            ;

        Assert.NotNull(activity.SuggestedActions);
        Assert.Same(suggestedActions, activity.SuggestedActions);
        Assert.Single(activity.SuggestedActions.Actions);

        Assert.NotNull(activity.SuggestedActions);
        Assert.Empty(activity.SuggestedActions.To);
    }

    [Fact]
    public void MessageActivity_SuggestedActions_RoundTrip()
    {
        MessageActivity activity = new("Choose");
        activity.SuggestedActions = new SuggestedActions();
        activity.SuggestedActions.AddRecipients("user1");
        activity.SuggestedActions.AddActions(
            new SuggestedAction(ActionTypes.OpenUrl, "Open", "https://example.com"),
            new SuggestedAction(ActionTypes.IMBack, "Say Hi", "hi")
        );

        string json = activity.ToJson();

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        MessageActivity roundTripped = MessageActivity.FromActivity(coreActivity);

        Assert.NotNull(roundTripped.SuggestedActions);
        Assert.Single(roundTripped.SuggestedActions.To);
        Assert.Equal("user1", roundTripped.SuggestedActions.To[0]);
        Assert.Equal(2, roundTripped.SuggestedActions.Actions.Count);
        Assert.Equal("openUrl", roundTripped.SuggestedActions.Actions[0].Type!.ToString());
        Assert.Equal("Open", roundTripped.SuggestedActions.Actions[0].Title);
        Assert.Equal("imBack", roundTripped.SuggestedActions.Actions[1].Type!.ToString());
        Assert.Equal("Say Hi", roundTripped.SuggestedActions.Actions[1].Title);
    }
}
