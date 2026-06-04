// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable ExperimentalTeamsSuggestedAction

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class SuggestedActionSubmitHandlerTests
{
    [Fact]
    public void InvokeNames_SuggestedActionSubmit_HasExpectedValue()
    {
        Assert.Equal("suggestedActions/submit", InvokeNames.SuggestedActionSubmit);
    }

    [Fact]
    public void Register_SuggestedActionSubmitRoute_Succeeds()
    {
        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.SuggestedActionSubmit),
            Selector = activity => activity.Name == InvokeNames.SuggestedActionSubmit,
        });

        Assert.Single(router.GetRoutes());
    }

    [Fact]
    public void SuggestedActionSubmit_RouteSelector_MatchesCorrectInvokeName()
    {
        InvokeActivity activity = new(InvokeNames.SuggestedActionSubmit)
        {
            Value = new JsonObject { ["vote"] = "approve" }
        };

        bool matches = activity.Name == InvokeNames.SuggestedActionSubmit;
        Assert.True(matches);
    }

    [Fact]
    public void SuggestedActionSubmit_RouteSelector_DoesNotMatchOtherInvoke()
    {
        InvokeActivity activity = new(InvokeNames.AdaptiveCardAction);

        bool matches = activity.Name == InvokeNames.SuggestedActionSubmit;
        Assert.False(matches);
    }

    [Fact]
    public void SuggestedActionSubmit_InvokeActivity_ValueRoundTrips()
    {
        var payload = new { vote = "approve", reason = "looks good" };
        InvokeActivity activity = new(InvokeNames.SuggestedActionSubmit)
        {
            Value = JsonSerializer.SerializeToNode(payload)
        };

        Assert.NotNull(activity.Value);
        Assert.Equal("approve", activity.Value!["vote"]!.GetValue<string>());
        Assert.Equal("looks good", activity.Value["reason"]!.GetValue<string>());
    }

    [Fact]
    public void OutgoingMessage_WithActionSubmitSuggestedAction_SerializesToSpecShape()
    {
        MessageActivity activity = new("Approve or reject:")
        {
            SuggestedActions = new SuggestedActions()
        };
        activity.SuggestedActions.AddAction(
            new SuggestedAction(ActionType.Submit, "Approve", new { vote = "approve" })
        );

        string json = activity.ToJson();
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement action = doc.RootElement.GetProperty("suggestedActions").GetProperty("actions")[0];
        Assert.Equal("Action.Submit", action.GetProperty("type").GetString());
        Assert.Equal("Approve", action.GetProperty("title").GetString());
        Assert.Equal("approve", action.GetProperty("value").GetProperty("vote").GetString());
    }

    [Fact]
    public void InvokeActivity_FromCoreActivity_ParsesSuggestedActionSubmit()
    {
        string json = """
        {
          "type": "invoke",
          "name": "suggestedActions/submit",
          "value": { "vote": "reject" }
        }
        """;

        CoreActivity coreActivity = CoreActivity.FromJsonString(json);
        InvokeActivity activity = InvokeActivity.FromActivity(coreActivity);

        Assert.Equal("suggestedActions/submit", activity.Name);
        Assert.NotNull(activity.Value);
        Assert.Equal("reject", activity.Value!["vote"]!.GetValue<string>());
    }
}
