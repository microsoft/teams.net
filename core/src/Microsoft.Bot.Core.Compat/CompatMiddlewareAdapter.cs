using Microsoft.Bot.Builder;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.Compat;

internal sealed class CompatMiddlewareAdapter(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        using var turnContext = new TurnContext(new CompatBotAdapter(botApplication), activity.ToCompatActivity());
        return bfMiddleWare.OnTurnAsync(turnContext, (activity)
                => nextTurn(cancellationToken), cancellationToken);
    }
}