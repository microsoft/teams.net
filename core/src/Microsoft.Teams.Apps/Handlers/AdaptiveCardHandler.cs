// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling adaptive card action invoke activities.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
public delegate Task<InvokeResponse> AdaptiveCardActionHandler(Context<InvokeActivity<AdaptiveCardActionValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering adaptive card action invoke handlers.
/// </summary>
public static class AdaptiveCardExtensions
{
    /// <summary>
    /// Registers a handler for adaptive card action invoke activities.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously a catch-all invoke handler could be registered alongside specific invoke handlers. This combination now throws at registration time.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAdaptiveCardAction(this TeamsBotApplication app, AdaptiveCardActionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.AdaptiveCardAction),
            Selector = activity => activity.Name == InvokeNames.AdaptiveCardAction,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<AdaptiveCardActionValue> typedActivity = new(ctx.Activity);
                Context<InvokeActivity<AdaptiveCardActionValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
