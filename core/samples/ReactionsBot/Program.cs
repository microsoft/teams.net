// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Core;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage("(?i)^help$", async (context, cancellationToken) =>
{
    await context.SendAsync(
        """
        **ReactionsBot**

        Commands:
        - `react` - Send a message, add two reactions, then remove one reaction
        - `help` - Show this message
        """,
        cancellationToken);
});

teamsApp.OnMessage("(?i)^react$", async (context, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(context.Activity.Conversation);

    MessageActivityInput message = MessageActivityInput.CreateBuilder()
        .WithText("Adding and removing reactions on this message...")
        .Build();

    SendActivityResponse? response = await context.SendAsync(message, cancellationToken);
    string activityId = response?.Id ?? throw new InvalidOperationException("SendActivityResponse.Id is required.");

    await Task.Delay(2000, cancellationToken);

    await context.Api.Conversations.AddReactionAsync(
        context.Activity.Conversation.Id,
        activityId,
        "1f44b_wavinghand-tone4",
        cancellationToken: cancellationToken);

    await Task.Delay(2000, cancellationToken);

    await context.Api.Conversations.AddReactionAsync(
        context.Activity.Conversation.Id,
        activityId,
        "1f601_beamingfacewithsmilingeyes",
        cancellationToken: cancellationToken);

    await Task.Delay(2000, cancellationToken);

    await context.Api.Conversations.DeleteReactionAsync(
        context.Activity.Conversation.Id,
        activityId,
        "1f601_beamingfacewithsmilingeyes",
        cancellationToken: cancellationToken);
});

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    await context.SendAsync("Send `react` to run the reaction flow.", cancellationToken);
});

webApp.Run();
