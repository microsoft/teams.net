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
internal sealed class TurnMiddleware : ITurnMiddleware, IEnumerable<ITurnMiddleware>
{
    private ITurnMiddleware[] _frozen = [];
    private readonly List<ITurnMiddleware> _building = [];
    private bool _isFrozen;

    /// <summary>
    /// Adds a middleware component to the end of the pipeline.
    /// Throws <see cref="InvalidOperationException"/> if called after <see cref="Freeze"/>.
    /// </summary>
    /// <param name="middleware">The middleware to add. Cannot be null.</param>
    /// <returns>The current TurnMiddleware instance for method chaining.</returns>
    internal TurnMiddleware Use(ITurnMiddleware middleware)
    {
        if (_isFrozen)
            throw new InvalidOperationException(
                "Middleware cannot be added after the pipeline has been frozen (A-020). " +
                "Register all middleware before the application starts.");

        _building.Add(middleware);
        return this;
    }

    /// <summary>
    /// Seals the middleware list and converts it to a read-only array. Call this once during
    /// application startup (e.g. from IHostedService.StartAsync) to enforce the invariant that
    /// no middleware is registered after the first request is processed (A-020).
    /// Subsequent calls to <see cref="Use"/> will throw <see cref="InvalidOperationException"/>.
    /// </summary>
    internal void Freeze()
    {
        _frozen = [.. _building];
        _isFrozen = true;
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
    /// Uses the frozen array when frozen (fast path); falls back to the building list
    /// during startup before Freeze() is called.
    /// </summary>
    public Task RunPipelineAsync(BotApplication botApplication, CoreActivity activity, Func<CoreActivity, CancellationToken, Task>? callback, int nextMiddlewareIndex, CancellationToken cancellationToken)
    {
        IList<ITurnMiddleware> middlewares = _isFrozen ? _frozen : _building;
        if (nextMiddlewareIndex == middlewares.Count)
        {
            return callback is not null ? callback!(activity, cancellationToken) ?? Task.CompletedTask : Task.CompletedTask;
        }
        ITurnMiddleware nextMiddleware = middlewares[nextMiddlewareIndex];
        return nextMiddleware.OnTurnAsync(
            botApplication,
            activity,
            (ct) => RunPipelineAsync(botApplication, activity, callback, nextMiddlewareIndex + 1, ct),
            cancellationToken);
    }

    public IEnumerator<ITurnMiddleware> GetEnumerator()
    {
        IList<ITurnMiddleware> middlewares = _isFrozen ? _frozen : _building;
        return middlewares.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
