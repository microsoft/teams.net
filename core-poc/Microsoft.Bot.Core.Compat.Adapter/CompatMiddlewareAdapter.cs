using Microsoft.Bot.Builder;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.Compat.Adapter;

internal class CompatMiddlewareAdapter(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, Core.NextDelegate next, CancellationToken cancellationToken = default)
        => bfMiddleWare.OnTurnAsync(new TurnContext(new CompatBotAdapter(botApplication), activity.ToCompatActivity()), (activity)
            => next(cancellationToken), cancellationToken);
}