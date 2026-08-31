// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

using Microsoft.Teams.Apps;
using Microsoft.Teams.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

QuotingHandlers.Register(teamsApp);
ThreadingHandlers.Register(teamsApp);
ReactionHandlers.Register(teamsApp);

teamsApp.OnMessage("(?i)^help$", async (context, cancellationToken) =>
{
    await context.SendAsync(
        new MessageActivityInput().WithText(
            """
            **Interacting with Messages**

            **Quoting:**
            - `quote reply` - explicitly quote your message
            - `quote message` - quote a previously sent message
            - `quote add` - compose a quote with the message builder
            - `quote batch` - combine multiple quotes
            - `quote manual` - combine a quote and text manually

            **Threading:**
            - `thread reply` - send a reactive threaded reply
            - `thread send` - send to the same thread without quoting
            - `thread proactive` - send a proactive threaded reply
            - `thread manual` - construct a threaded conversation ID manually

            **Reactions:**
            - `reaction add <type>` - add a reaction to your message
            - `reaction remove <type>` - add, then remove, a reaction
            - `reaction proactive` - send a bot message and react to it using app-level APIs

            Quote or react to one of my messages to see the corresponding inbound event.
            """,
            TextFormats.Markdown),
        cancellationToken);
});

teamsApp.OnMessage("(?s)^.*$", async (context, cancellationToken) =>
{
    if (await QuotingHandlers.HandleQuotedMessageAsync(context, cancellationToken))
    {
        return;
    }

    string text = context.Activity.TextWithoutMentions ?? "";
    if (Regex.IsMatch(
        text,
        @"(?i)^(help|quote (reply|message|add|batch|manual)|thread (send|reply|proactive|manual)|reaction (add \S+|remove \S+|proactive))$"))
    {
        return;
    }

    await context.SendAsync("Send `help` to see the available commands.", cancellationToken);
});

webApp.Run();
