// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling adaptive card action invoke activities with strongly-typed response.
/// </summary>
public delegate Task<InvokeResponse> AdaptiveCardActionHandler(Context<InvokeActivity<AdaptiveCardActionValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering adaptive card action invoke handlers.
/// </summary>
public static class AdaptiveCardExtensions
{
    /// <summary>
    /// Registers a handler for adaptive card action invoke activities with strongly-typed response.
    /// </summary>
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
