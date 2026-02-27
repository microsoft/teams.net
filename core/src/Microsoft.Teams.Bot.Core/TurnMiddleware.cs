// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core;

/// <summary>
/// Manages and executes a middleware pipeline for processing bot turns.
/// </summary>
/// <remarks>
/// This class implements a chain of responsibility pattern where each middleware component can process
/// an activity before passing control to the next middleware in the pipeline. The pipeline executes
/// sequentially, with each middleware having the opportunity to modify the activity, perform side effects,
/// or short-circuit the pipeline. Middleware is executed in the order it was registered via the Use method.
/// </remarks>
internal sealed class TurnMiddleware : ITurnMiddleWare, IEnumerable<ITurnMiddleWare>
{
    private readonly IList<ITurnMiddleWare> _middlewares = [];

    /// <summary>
    /// Adds a middleware component to the end of the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add. Cannot be null.</param>
    /// <returns>The current TurnMiddleware instance for method chaining.</returns>
    internal TurnMiddleware Use(ITurnMiddleWare middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Processes a turn by executing the middleware pipeline.
    /// </summary>
    /// <param name="botApplication">The bot application processing the turn.</param>
    /// <param name="activity">The activity to process.</param>
    /// <param name="next">Delegate to invoke the next middleware in the outer pipeline.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous pipeline execution.</returns>
    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn next, CancellationToken cancellationToken = default)
    {
        await RunPipelineAsync(botApplication, activity, null!, 0, cancellationToken).ConfigureAwait(false);
        await next(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Recursively executes the middleware pipeline starting from the specified index.
    /// </summary>
    /// <param name="botApplication">The bot application processing the turn.</param>
    /// <param name="activity">The activity to process.</param>
    /// <param name="callback">Optional callback to invoke after all middleware has executed.</param>
    /// <param name="nextMiddlewareIndex">The index of the next middleware to execute in the pipeline.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous pipeline execution.</returns>
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
