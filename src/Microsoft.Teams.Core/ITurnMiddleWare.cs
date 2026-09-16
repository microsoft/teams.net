// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core;

/// <summary>
/// Represents a delegate that invokes the next middleware component in the pipeline asynchronously.
/// </summary>
/// <remarks>This delegate is typically used in middleware scenarios to advance the request processing pipeline.
/// The cancellation token should be observed to support cooperative cancellation.</remarks>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the completion of the middleware invocation.</returns>
public delegate Task NextTurn(CancellationToken cancellationToken);

/// <summary>
/// Defines a middleware component that can process or modify activities during a bot turn.
/// </summary>
/// <remarks>Implement this interface to add custom logic before or after the bot processes an activity.
/// Middleware can perform tasks such as logging, authentication, or altering activities. Multiple middleware components
/// can be chained together; each should call the nextTurn delegate to continue the pipeline.</remarks>
public interface ITurnMiddleware
{
    /// <summary>
    /// Triggers the middleware to process an activity during a bot turn.
    /// </summary>
    /// <param name="botApplication">The bot application processing the current turn.</param>
    /// <param name="activity">The incoming activity to process.</param>
    /// <param name="nextTurn">A delegate that invokes the next middleware in the pipeline. Call this to continue processing.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous middleware execution.</returns>
    Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default);
}
