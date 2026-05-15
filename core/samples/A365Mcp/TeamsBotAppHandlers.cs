// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

namespace A365Mcp;

internal static class TeamsBotAppHandlers
{
    public static TeamsBotApplication RegisterHandlers(this TeamsBotApplication teamsApp, IServiceProvider services)
    {
        var agent = services.GetRequiredService<Agent>();

        teamsApp.OnMessage(async (context, cancellationToken) =>
        {
            await context.SendTypingActivityAsync(cancellationToken);
            string userText = context.Activity.TextWithoutMentions ?? "";
            await RespondAsync(agent, context, userText, cancellationToken);
        });
        return teamsApp;
    }

    private static async Task RespondAsync(Agent agent, Context<MessageActivity> context, string userText, CancellationToken cancellationToken)
    {
        var response = await agent.RunAsync(
            context.Activity?.Conversation?.Id!,
            userText,
            context.Activity?.Recipient?.GetAgenticIdentity(),
            cancellationToken);

        var responseMessage = TeamsActivity.CreateBuilder()
            .WithText(response, TextFormats.Markdown)
            .Build();

        await context.SendActivityAsync(responseMessage, cancellationToken);
    }
}
