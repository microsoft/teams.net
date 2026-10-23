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
            - `quote reply` - auto-quote your message
            - `quote message` - quote a previously sent message
            - `quote add` - compose a quote with the message builder
            - `quote batch` - combine multiple quotes
            - `quote manual` - combine a quote and text manually

            **Threading:**
            - `thread reply` - send a reactive, quoted reply (in a channel, in the same thread; in a 1:1 chat, matches the level of your message)
            - `thread send` - send reactively without quoting (in a channel, in the same thread; in a 1:1 chat, matches the level of your message)
            - `thread proactive` - send a proactive reply in a thread on your message
            - `thread top-level` - send a new top-level message, outside any reply chain (thread)

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
        @"(?i)^(help|quote (reply|message|add|batch|manual)|thread (send|reply|proactive|top-level)|reaction (add \S+|remove \S+|proactive))$"))
    {
        return;
    }

    await context.SendAsync("Send `help` to see the available commands.", cancellationToken);
});

webApp.Run();
