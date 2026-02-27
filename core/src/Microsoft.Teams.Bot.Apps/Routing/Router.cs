// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Routing;

/// <summary>
/// Router for dispatching Teams activities to registered routes
/// </summary>
// TODO : add inline docs to handlers for breaking change
internal sealed class Router
{
    private readonly List<RouteBase> _routes = [];
    private readonly ILogger _logger;

    internal Router(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Routes registered in the router.
    /// </summary>
    public IReadOnlyList<RouteBase> GetRoutes() => _routes.AsReadOnly();

    /// <summary>
    /// Registers a route. Routes are checked and invoked in registration order.
    /// For non-invoke activities all matching routes run sequentially.
    /// For invoke activities — routes must be non-overlapping.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a route with the same name is already registered, or if an invoke catch-all
    /// is mixed with specific invoke handlers.
    /// </exception>
    public Router Register<TActivity>(Route<TActivity> route) where TActivity : TeamsActivity
    {
        if (_routes.Any(r => r.Name == route.Name))
        {
            throw new InvalidOperationException($"A route with name '{route.Name}' is already registered.");
        }

        string invokePrefix = TeamsActivityType.Invoke + "/";

        if (route.Name == TeamsActivityType.Invoke && _routes.Any(r => r.Name.StartsWith(invokePrefix, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Cannot register a catch-all invoke handler when specific invoke handlers are already registered. Use specific handlers or handle all invoke types inside OnInvoke.");
        }

        if (route.Name.StartsWith(invokePrefix, StringComparison.Ordinal) && _routes.Any(r => r.Name == TeamsActivityType.Invoke))
        {
            throw new InvalidOperationException($"Cannot register '{route.Name}' when a catch-all invoke handler is already registered. Remove OnInvoke or use specific handlers exclusively.");
        }
        _routes.Add(route);
        return this;
    }

    /// <summary>
    /// Dispatches the activity to all matching routes in registration order.
    /// </summary>
    public async Task DispatchAsync(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var matchingRoutes = _routes.Where(r => r.Matches(ctx.Activity)).ToList();

        if (matchingRoutes.Count == 0 && _routes.Count>0)
        {
            _logger.LogTrace("No routes matched activity of type '{Type}'.", ctx.Activity.Type);
            return;
        }

        foreach (var route in matchingRoutes)
        {
            _logger.LogTrace("Dispatching '{Type}' activity to route '{Name}'.", ctx.Activity.Type, route.Name);
            await route.InvokeRoute(ctx, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Dispatches the specified activity context to the first matching route and returns the result of the invocation.
    /// </summary>
    /// <param name="ctx">The activity context to dispatch. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a response object with the outcome
    /// of the invocation.</returns>
    public async Task<InvokeResponse> DispatchWithReturnAsync(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var matchingRoutes = _routes.Where(r => r.Matches(ctx.Activity)).ToList();

        if (matchingRoutes.Count == 0 && _routes.Count > 0)
        {
            _logger.LogWarning("No routes matched invoke activity of type '{Type}'; handler will not execute.", ctx.Activity.Type);
            return null!; // TODO : return appropriate response
        }

        return await matchingRoutes[0].InvokeRouteWithReturn(ctx, cancellationToken).ConfigureAwait(false);
    }

}
