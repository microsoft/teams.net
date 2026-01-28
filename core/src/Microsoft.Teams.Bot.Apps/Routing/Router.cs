// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Routing;

/// <summary>
/// Router for dispatching Teams activities to registered routes
/// </summary>
public class Router
{
    private readonly List<RouteBase> _routes = [];

    /// <summary>
    /// Routes registered in the router.
    /// </summary>
    public IReadOnlyList<RouteBase> GetRoutes() => _routes.AsReadOnly();

    /// <summary>
    /// Registers a route. Routes are checked in registration order.
    /// IMPORTANT: Register specific routes before general catch-all routes.
    /// </summary>
    public Router Register<TActivity>(Route<TActivity> route) where TActivity : TeamsActivity
    {
        _routes.Add(route);
        return this;
    }

    /// <summary>
    /// Selects the first matching route for the given activity.
    /// </summary>
    public Route<TActivity>? Select<TActivity>(TActivity activity) where TActivity : TeamsActivity
    {
        return _routes
            .OfType<Route<TActivity>>()
            .FirstOrDefault(r => r.Selector(activity));
    }

    /// <summary>
    /// Selects all matching routes for the given activity.
    /// </summary>
    public IEnumerable<Route<TActivity>> SelectAll<TActivity>(TActivity activity) where TActivity : TeamsActivity
    {
        return _routes
            .OfType<Route<TActivity>>()
            .Where(r => r.Selector(activity));
    }

    /// <summary>
    /// Dispatches the activity to the first matching route.
    /// Routes are checked in registration order.
    /// </summary>
    public async Task DispatchAsync(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // TODO : support multiple routes?
        foreach (var route in _routes)
        {
            if (route.Matches(ctx.Activity))
            {
                await route.InvokeRoute(ctx, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
    }

    /// <summary>
    /// Dispatches the specified activity context to all matching routes and returns the result of the invocation.
    /// </summary>
    /// <param name="ctx">The activity context to dispatch. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a response object with the outcome
    /// of the invocation.</returns>
    public async Task<CoreInvokeResponse> DispatchWithReturnAsync(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // TODO : support multiple routes?
        foreach (var route in _routes)
        {
            if (route.Matches(ctx.Activity))
            {
                return await route.InvokeRouteWithReturn(ctx, cancellationToken).ConfigureAwait(false);
            }
        }

        return null!; // TODO : return appropriate response
    }
}
