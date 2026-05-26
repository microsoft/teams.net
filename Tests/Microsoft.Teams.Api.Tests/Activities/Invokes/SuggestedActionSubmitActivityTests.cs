#pragma warning disable ExperimentalTeamsSuggestedAction

using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using MessageActivity = Microsoft.Teams.Api.Activities.MessageActivity;

namespace Microsoft.Teams.Api.Tests.Activities;

public class SuggestedActionSubmitActivityTests
{
    [Fact]
    public void SuggestedActionSubmitActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SuggestedActionSubmitActivity.json");

        var activity = JsonSerializer.Deserialize<SuggestedActionSubmitActivity>(json);

        Assert.NotNull(activity);
        Assert.Equal("suggestedActionSubmitId", activity!.Id);
        Assert.Equal("channelId", activity.ChannelId.Value);
        Assert.Equal("suggestedActions/submit", activity.Name.Value);
        var value = Assert.IsType<JsonElement>(activity.Value);
        Assert.Equal(JsonValueKind.Object, value.ValueKind);
        Assert.Equal("approve", value.GetProperty("vote").GetString());
    }

    [Fact]
    public void SuggestedActionSubmitActivity_JsonDeserialize_DispatchedFromInvokeBase()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SuggestedActionSubmitActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);

        Assert.IsType<SuggestedActionSubmitActivity>(activity);
    }

    [Fact]
    public void SuggestedActionSubmitActivity_JsonDeserialize_DispatchedFromActivityBase()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SuggestedActionSubmitActivity.json");

        var activity = JsonSerializer.Deserialize<Activity>(json);

        Assert.IsType<SuggestedActionSubmitActivity>(activity);
    }

    [Fact]
    public void SuggestedActionSubmitActivity_GetPath()
    {
        var activity = new SuggestedActionSubmitActivity();

        Assert.Equal("Activity.Invoke.SuggestedActions/submit", activity.GetPath());
    }

    [Fact]
    public void OutgoingMessage_WithActionSubmitSuggestedAction_SerializesToSpecShape()
    {
        // Verifies the wire format the bot produces when sending an Action.Submit
        // suggested action. Per the design spec the platform expects:
        //   suggestedActions.actions[i].type  == "Action.Submit"
        //   suggestedActions.actions[i].title == button label
        //   suggestedActions.actions[i].value == structured payload (object)
        var message = new MessageActivity("Approve or reject:")
        {
            SuggestedActions = new SuggestedActions
            {
                Actions =
                {
                    new Cards.Action(Cards.ActionType.Submit) { Title = "Approve", Value = new { vote = "approve" } }
                }
            }
        };

        var json = JsonSerializer.Serialize(message);
        using var doc = JsonDocument.Parse(json);

        var action = doc.RootElement.GetProperty("suggestedActions").GetProperty("actions")[0];
        Assert.Equal("Action.Submit", action.GetProperty("type").GetString());
        Assert.Equal("Approve", action.GetProperty("title").GetString());
        Assert.Equal("approve", action.GetProperty("value").GetProperty("vote").GetString());
    }
}
