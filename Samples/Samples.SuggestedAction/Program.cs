using System.Text.Json;

using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage(async (context, cancellationToken) =>
{
    await context.Send(
        "Send me an Action.Submit suggested action invoke (name: \"suggestedAction/submit\") and I'll echo the value back.",
        cancellationToken);
});

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
