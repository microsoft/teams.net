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

// On any user message, reply with two Action.Submit suggested-action chips.
// When the user clicks one, the platform dispatches a "suggestedAction/submit"
// invoke whose Value is the action's Value object — handled below.
teams.OnMessage(async (context, cancellationToken) =>
{
#pragma warning disable ExperimentalTeamsSuggestedAction
    var reply = new MessageActivity("Approve or reject the request:")
    {
        SuggestedActions = new SuggestedActions
        {
            Actions =
            {
                new CardAction(CardActionType.Submit) { Title = "Approve", Value = new { vote = "approve" } },
                new CardAction(CardActionType.Submit) { Title = "Reject", Value = new { vote = "reject" } }
            }
        }
    };
#pragma warning restore ExperimentalTeamsSuggestedAction

    await context.Send(reply, cancellationToken);
});

// OnSuggestedActionSubmit and SuggestedActionSubmitActivity are marked
// [Experimental("ExperimentalTeamsSuggestedAction")]. The C# compiler emits
// that diagnostic as an error at the call site, so the build fails unless
// you opt in. The #pragma below opts in for this single usage; for a
// project-wide opt-in, add NoWarn in the .csproj instead — see README.md.
#pragma warning disable ExperimentalTeamsSuggestedAction
teams.OnSuggestedActionSubmit(async (context, cancellationToken) =>
{
    context.Log.Info("[SUGGESTED_ACTION_SUBMIT] activity received");

    var serializedValue = context.Activity.Value is JsonElement value
        ? value.GetRawText()
        : "<none>";

    context.Log.Info($"[SUGGESTED_ACTION_SUBMIT] value={serializedValue}");
    await context.Send($"Got suggestedAction/submit with value: {serializedValue}", cancellationToken);
});
#pragma warning restore ExperimentalTeamsSuggestedAction

app.Run();
