// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

internal class WelcomeMessageMiddleware : ITurnMiddleware
{
    private bool _hasSentWelcomeMessage = false;

    internal const string WelcomeMessage =
"""
**Teams Bot Demo**

**Messages**
- `hello` - Greeting
- `markdown` - Markdown formatting demo
- `citation` - AI citations with feedback
- `targeted` - Targeted message lifecycle(send, update, delete)
- `react` - Bot reactions(add, remove)
- `card` - Send an Adaptive Card with a feedback form
- `feedback` - Feedback form with Adaptive Card action round-trip
- `task` - Open a task module dialog

** Commands**
- `/help` - Available slash commands
- `/about` - About this bot
- `/time` - Current server time

** Lifecycle** *(automatic)*
- Message edits, deletes, and reactions are detected
- Member join/leave and install/uninstall events are handled
""";

    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        if (!_hasSentWelcomeMessage)
        {
            var welcomeActivity = TeamsActivity.CreateBuilder()
                .WithType("message")
                .WithText(WelcomeMessage, TextFormats.Markdown)
                .WithConversationReference(TeamsActivity.FromActivity(activity))
                .Build();

            await botApplication.SendActivityAsync(welcomeActivity, cancellationToken);

            _hasSentWelcomeMessage = true;
        }
        if (nextTurn is not null)
        {
            await nextTurn(cancellationToken);
        }
    }
}
