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

    public async Task<InvokeResponse> RunPipelineAsync(BotApplication botApplication, CoreActivity activity, Func<CoreActivity, CancellationToken, Task<InvokeResponse>>? callback, int nextMiddlewareIndex, CancellationToken cancellationToken)
    {
        InvokeResponse invokeResponse = null!;
        if (nextMiddlewareIndex == _middlewares.Count)
        {
            if (callback is not null)
            {
                invokeResponse = await callback!(activity, cancellationToken).ConfigureAwait(false);
            }
            return invokeResponse;           
        }
        ITurnMiddleWare nextMiddleware = _middlewares[nextMiddlewareIndex];
        await nextMiddleware.OnTurnAsync(
            botApplication,
            activity,
            (ct) => RunPipelineAsync(botApplication, activity, callback, nextMiddlewareIndex + 1, ct),
            cancellationToken).ConfigureAwait(false);

        return invokeResponse;
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
