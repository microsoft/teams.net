using Microsoft.Bot.Builder;
using Rido.BFLite.Core;
using Rido.BFLite.Core.Schema;

namespace Rido.BFLite.Compat.Adapter;

internal class CompatMiddlewareAdapter(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, Activity activity, Core.NextDelegate next, CancellationToken cancellationToken = default)
        => bfMiddleWare.OnTurnAsync(new TurnContext(new CompatBotAdapter(botApplication), activity.ToCompatActivity()), (activity)
            => next(cancellationToken), cancellationToken);
}