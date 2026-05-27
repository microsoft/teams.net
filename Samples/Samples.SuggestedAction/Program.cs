// Action.Submit and the OnSuggestedActionSubmit handler are marked
// [Experimental("ExperimentalTeamsSuggestedAction")]. See README.md.
#pragma warning disable ExperimentalTeamsSuggestedAction

using System.Text.Json;

using Microsoft.Teams.Api;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using CardAction = Microsoft.Teams.Api.Cards.Action;
using CardActionType = Microsoft.Teams.Api.Cards.ActionType;
using MessageActivity = Microsoft.Teams.Api.Activities.MessageActivity;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();

// Reply to any user message with two Action.Submit suggested-action chips.
teams.OnMessage(async (context, cancellationToken) =>
{
    var reply = new MessageActivity("Approve or reject the request:")
    {
        SuggestedActions = new SuggestedActions
        {
            Actions =
            {
                new CardAction(CardActionType.Submit) { Title = "Approve", Value = new { vote = "approve" } },
                new CardAction(CardActionType.Submit) { Title = "Reject",  Value = new { vote = "reject" } }
            }
        }
    };

    await context.Send(reply, cancellationToken);
});

// Handle the resulting suggestedActions/submit invoke when the user clicks a chip.
teams.OnSuggestedActionSubmit(async (context, cancellationToken) =>
{
    var serializedValue = context.Activity.Value is JsonElement value
        ? value.GetRawText()
        : "<none>";

    context.Log.Info($"[SUGGESTED_ACTION_SUBMIT] value={serializedValue}");
    await context.Send($"Got suggestedActions/submit with value: {serializedValue}", cancellationToken);
});

app.Run();
