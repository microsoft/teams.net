// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;

using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core;

internal sealed class TurnMiddleware : ITurnMiddleWare, IEnumerable<ITurnMiddleWare>
{

    private readonly IList<ITurnMiddleWare> _middlewares = [];
    internal TurnMiddleware Use(ITurnMiddleWare middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }


    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn next, CancellationToken cancellationToken = default)
    {
        await RunPipelineAsync(botApplication, activity, null!, 0, cancellationToken).ConfigureAwait(false);
        await next(cancellationToken).ConfigureAwait(false);
    }

    public Task RunPipelineAsync(BotApplication botApplication, CoreActivity activity, Func<CoreActivity, CancellationToken, Task>? callback, int nextMiddlewareIndex, CancellationToken cancellationToken)
    {
        if (nextMiddlewareIndex == _middlewares.Count)
        {
            return callback is not null ? callback!(activity, cancellationToken) ?? Task.CompletedTask : Task.CompletedTask;
        }
        ITurnMiddleWare nextMiddleware = _middlewares[nextMiddlewareIndex];
        return nextMiddleware.OnTurnAsync(
            botApplication,
            activity,
            (ct) => RunPipelineAsync(botApplication, activity, callback, nextMiddlewareIndex + 1, ct),
            cancellationToken);

    }

    public IEnumerator<ITurnMiddleWare> GetEnumerator()
    {
        return _middlewares.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
