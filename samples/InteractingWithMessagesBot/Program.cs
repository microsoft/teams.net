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
            - `default send` - send with the default placement for this scope, without quoting
            - `thread proactive` - send a proactive threaded reply
            - `thread proactive quote` - explicitly place and quote a threaded reply
            - `thread proactive targeted` - send a proactive targeted threaded reply
            - `thread proactive targeted quote` - send a proactive targeted threaded reply with a quote

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
        @"(?i)^(help|quote (reply|message|add|batch|manual)|default send|thread proactive( quote| targeted( quote)?)?|reaction (add \S+|remove \S+|proactive))$"))
    {
        return;
    }

    await context.SendAsync("Send `help` to see the available commands.", cancellationToken);
});

webApp.Run();
