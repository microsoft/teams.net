using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class CardButtonClickedMEActivityTests
{
    private CardButtonClickedActivity setupCardButtonClickedActivity()
    {
        var anyValueObject = new Parameter()
        {
            Name = "Somelist",
            Value = "Toronto"
        };
        return new CardButtonClickedActivity()
        {          
            Value = anyValueObject,
            Conversation = new Conversation()
            {
                Id = "19:someid",
                Type = ConversationType.Personal 
            },
            ServiceUrl = "https://url-value"
        };
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonSerialize()
    {
        var activity = setupCardButtonClickedActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/onCardButtonClicked";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json"
        ), json);
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = setupCardButtonClickedActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/onCardButtonClicked";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json"
        ), json);
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = setupCardButtonClickedActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/onCardButtonClicked";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json"
        ), json);
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json");
        var activity = JsonSerializer.Deserialize<CardButtonClickedActivity>(json);
        var expected = setupCardButtonClickedActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        string expectedPath = "Activity.Invoke.ComposeExtension/onCardButtonClicked";
        Assert.Equal(expectedPath, activity.GetPath());
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = setupCardButtonClickedActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'CardButtonClickedActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = setupCardButtonClickedActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void CardButtonClickedMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CardButtonClickedMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = setupCardButtonClickedActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        string expectedPath = "Activity.Invoke.ComposeExtension/onCardButtonClicked";
        Assert.Equal(expectedPath, activity.GetPath());
    }
}
