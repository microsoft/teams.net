// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Routing;

/// <summary>
/// Router for dispatching Teams activities to registered routes
/// </summary>
public class Router
{
    private readonly List<RouteBase> _routes = [];
    private readonly ILogger<Router> _logger;

    /// <summary>
    /// Initializes a new instance of the Router class.
    /// </summary>
    /// <param name="logger">Logger for router diagnostics. Optional.</param>
    public Router(ILogger<Router>? logger = null)
    {
        _logger = logger ?? NullLogger<Router>.Instance;
    }

    /// <summary>
    /// Routes registered in the router.
    /// </summary>
    public IReadOnlyList<RouteBase> GetRoutes() => _routes.AsReadOnly();

    /// <summary>
    /// Registers a route. Routes are checked in registration order.
    /// IMPORTANT: Register specific routes before general catch-all routes.
    /// Call Next() in handlers to continue to the next matching route.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    public Router Register<TActivity>(Route<TActivity> route) where TActivity : TeamsActivity
    {
        _routes.Add(route);
        return this;
    }

    /// <summary>
    /// Dispatches the activity to the first matching route.
    /// Routes are checked in registration order.
    /// </summary>
    public async Task DispatchAsync(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var matchingRoutes = _routes.Where(r => r.Matches(ctx.Activity)).ToList();

        if (matchingRoutes.Count == 0)
        {
            _logger.LogDebug(
                "No routes matched activity type '{Type}'",
                ctx.Activity.Type
            );
            return;
        }

        if (matchingRoutes.Count > 1)
        {
            _logger.LogWarning(
                "Activity type '{Type}' matched {Count} routes: [{Routes}]. Only the first route will execute without Next().",
                ctx.Activity.Type,
                matchingRoutes.Count,
                string.Join(", ", matchingRoutes.Select(r => r.Name))
            );
        }

        await matchingRoutes[0].InvokeRoute(ctx, cancellationToken).ConfigureAwait(false);
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

        var matchingRoutes = _routes.Where(r => r.Matches(ctx.Activity)).ToList();

        if (matchingRoutes.Count == 0)
        {
            _logger.LogWarning(
                "No routes matched activity type '{Type}'",
                ctx.Activity.Type
            );
            return null!; // TODO : return appropriate response
        }

        if (matchingRoutes.Count > 1)
        {
            _logger.LogWarning(
                "Activity type '{Type}' matched {Count} routes: [{Routes}]. Only the first route will execute without Next().",
                ctx.Activity.Type,
                matchingRoutes.Count,
                string.Join(", ", matchingRoutes.Select(r => r.Name))
            );
        }

        return await matchingRoutes[0].InvokeRouteWithReturn(ctx, cancellationToken).ConfigureAwait(false);
    }
}
