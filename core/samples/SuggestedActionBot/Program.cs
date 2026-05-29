// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// Reply to any user message with two Action.Submit suggested-action chips.
teamsApp.OnMessage(async (context, cancellationToken) =>
{
    MessageActivity reply = new("Approve or reject the request:")
    {
        SuggestedActions = new SuggestedActions()
        {
            To = [context.Activity.From?.Id!],
            Actions = [
                new SuggestedAction(ActionType.Submit, "Approve", new { vote = "approve" }),
                new SuggestedAction(ActionType.Submit, "Reject", new { vote = "reject" }),
            ]
        }
    };

    await context.SendActivityAsync(reply, cancellationToken);
});

// Handle the resulting suggestedActions/submit invoke when the user clicks a chip.
teamsApp.OnSuggestedActionSubmit(async (context, cancellationToken) =>
{
    string serializedValue = context.Activity.Value is { } value
        ? value.ToJsonString()
        : "<none>";

    context.Log.Info($"[SUGGESTED_ACTION_SUBMIT] value={serializedValue}");
    await context.SendActivityAsync(
        new MessageActivity($"Got suggestedActions/submit with value: {serializedValue}"),
        cancellationToken);

    return new InvokeResponse(200);
});

webApp.Run();
