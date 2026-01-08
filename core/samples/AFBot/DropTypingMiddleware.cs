// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Schema;

namespace AFBot;

internal class DropTypingMiddleware : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        if (activity.Type == ActivityType.Typing) return Task.CompletedTask;
        return nextTurn(cancellationToken);
    }
}
