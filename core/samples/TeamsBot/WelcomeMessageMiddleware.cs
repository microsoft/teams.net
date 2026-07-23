// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

internal class WelcomeMessageMiddleware : ITurnMiddleware
{
    private bool _hasSentWelcomeMessage = false;

    internal const string WelcomeMessage =
"""
**Teams Bot Demo**

**Messages**
- `hello` - Greeting
- `extendedMarkdown` - Extended markdown demo (tables, math)
- `markdown` - Markdown formatting demo
- `citation` - AI citations with feedback
- `feedback` - Feedback form with Adaptive Card action round-trip

**Commands**
- `/help` - Available slash commands
- `/about` - About this bot
- `/time` - Current server time
""";

    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        if (!_hasSentWelcomeMessage)
        {
            MessageActivityInput welcomeActivity = MessageActivityInput.CreateBuilder()
                .WithText(WelcomeMessage, TextFormats.Markdown)
                .Build();

            await botApplication.ConversationClient.SendActivityAsync(activity.Conversation!.Id!, welcomeActivity, activity.ServiceUrl!, cancellationToken: cancellationToken);

            _hasSentWelcomeMessage = true;
        }
        if (nextTurn is not null)
        {
            await nextTurn(cancellationToken);
        }
    }
}
