// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Represents a method that handles an invocation request and returns a response asynchronously.
/// </summary>
/// <param name="context">The context for the invocation, containing request data and metadata required to process the operation. Cannot be
/// null.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. The default value is <see
/// cref="CancellationToken.None"/>.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the response to the invocation.</returns>
public delegate Task<InvokeResponse> InvokeHandler(Context<InvokeActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Provides extension methods for registering handlers for invoke activities in a Teams bot application.
/// </summary>
public static class InvokeExtensions
{
    /// <summary>
    /// Registers a handler for invoke activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The invoke handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnInvoke(this TeamsBotApplication app, InvokeHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = TeamsActivityType.Invoke,
            Selector = _ => true,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                return await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });
        return app;
    }
}
