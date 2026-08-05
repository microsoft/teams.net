// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// Obtain a standard ILogger from DI.
ILogger logger = webApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SuggestedActionBot");

// Reply to any user message with two Action.Submit suggested-action chips.
teamsApp.OnMessage(async (context, cancellationToken) =>
{
    MessageActivityInput reply = new MessageActivityInput()
        .WithText("Approve or reject the request:")
        .WithSuggestedActions(new SuggestedActions()
        {
            To = [context.Activity.From?.Id!],
            Actions = [
                new SuggestedAction(ActionTypes.Submit, "Approve", new { vote = "approve" }),
                new SuggestedAction(ActionTypes.Submit, "Reject", new { vote = "reject" }),
            ]
        })
        ;

    await context.SendAsync(reply, cancellationToken);
});

// Handle the resulting suggestedActions/submit invoke when the user clicks a chip.
teamsApp.OnSuggestedActionSubmit(async (context, cancellationToken) =>
{
    string serializedValue = context.Activity.Value is { } value
        ? value.ToJsonString()
        : "<none>";

    logger.LogInformation("[SUGGESTED_ACTION_SUBMIT] value={Value}", serializedValue);
    await context.SendAsync(
        new MessageActivityInput().WithText($"Got suggestedActions/submit with value: {serializedValue}"),
        cancellationToken);

    return new InvokeResponse(200);
});

webApp.Run();
