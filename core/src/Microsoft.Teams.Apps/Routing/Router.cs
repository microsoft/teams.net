// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Diagnostics;

namespace Microsoft.Teams.Apps.Routing;

/// <summary>
/// Router for dispatching Teams activities to registered routes
/// </summary>
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
        _logger.LogDebug("Registered route '{Name}' for activity type '{ActivityType}'.", route.Name, typeof(TActivity).Name);
        return this;
    }

    /// <summary>
    /// Dispatches the activity to all matching routes in registration order.
    /// </summary>
    public async Task DispatchAsync(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        _logger.LogDebug("Routing activity of type '{Type}' against {RouteCount} registered routes.", ctx.Activity.Type, _routes.Count);

        List<RouteBase> matchingRoutes = [];
        foreach (RouteBase route in _routes)
        {
            bool matched = route.Matches(ctx.Activity);
            _logger.LogTrace("Route '{Name}' selector returned {Result} for activity of type '{Type}'.", route.Name, matched, ctx.Activity.Type);
            if (matched)
            {
                matchingRoutes.Add(route);
            }
        }

        if (matchingRoutes.Count == 0 && _routes.Count > 0)
        {
            AppsTelemetry.HandlerUnmatched.Add(1, new KeyValuePair<string, object?>(AppsTelemetry.Tags.ActivityType, ctx.Activity.Type));
            _logger.LogWarning(
                "No routes matched activity of type '{Type}'.",
                ctx.Activity.Type
            );
            return;
        }

        _logger.LogDebug("Matched {MatchCount} route(s) for activity of type '{Type}'.", matchingRoutes.Count, ctx.Activity.Type);

        foreach (RouteBase route in matchingRoutes)
        {
            _logger.LogInformation("Dispatching '{Type}' activity to route '{Name}'.", ctx.Activity.Type, route.Name);
            _logger.LogTrace("Dispatching activity to route '{Name}': {Activity}", route.Name, ctx.Activity.ToJson());

            (string handlerType, string dispatch) = GetHandlerTags(route.Name);
            TagList handlerTags = new()
            {
                { AppsTelemetry.Tags.HandlerType, handlerType },
                { AppsTelemetry.Tags.HandlerDispatch, dispatch },
            };

            AppsTelemetry.HandlerDispatched.Add(1, handlerTags);

            using Activity? span = AppsTelemetry.Source.StartActivity(AppsTelemetry.Spans.Handler, ActivityKind.Internal);
            if (span is not null)
            {
                span.SetTag(AppsTelemetry.Tags.HandlerType, handlerType);
                span.SetTag(AppsTelemetry.Tags.HandlerDispatch, dispatch);
            }

            long startTimestamp = Stopwatch.GetTimestamp();
            try
            {
                await route.InvokeRoute(ctx, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppsTelemetry.HandlerFailures.Add(1, handlerTags);
                span.RecordException(ex);
                throw;
            }
            finally
            {
                double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
                AppsTelemetry.HandlerDuration.Record(elapsedMs, handlerTags);
            }

            _logger.LogDebug("Completed route '{Name}' for '{Type}' activity.", route.Name, ctx.Activity.Type);
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

        string? name = ctx.Activity is InvokeActivity inv ? inv.Name : null;

        _logger.LogDebug("Routing invoke activity with name '{Name}' against {RouteCount} registered routes.", name, _routes.Count);

        List<RouteBase> matchingRoutes = [];
        foreach (RouteBase route in _routes)
        {
            bool matched = route.Matches(ctx.Activity);
            _logger.LogTrace("Route '{RouteName}' selector returned {Result} for invoke '{Name}'.", route.Name, matched, name);
            if (matched)
            {
                matchingRoutes.Add(route);
            }
        }

        if (matchingRoutes.Count == 0 && _routes.Count > 0)
        {
            TagList unmatchedTags = new()
            {
                { AppsTelemetry.Tags.ActivityType, ctx.Activity.Type },
                { AppsTelemetry.Tags.InvokeName, name ?? string.Empty },
            };
            AppsTelemetry.HandlerUnmatched.Add(1, unmatchedTags);
            _logger.LogWarning("No routes matched invoke activity with name '{Name}'; returning 501.", name);
            return new InvokeResponse(501);
        }

        _logger.LogInformation("Dispatching invoke activity with name '{Name}' to route '{Route}'.", name, matchingRoutes[0].Name);
        _logger.LogTrace("Dispatching invoke activity to route '{Route}': {Activity}", matchingRoutes[0].Name, ctx.Activity.ToJson());

        (string handlerType, string dispatch) = GetHandlerTags(matchingRoutes[0].Name);
        TagList handlerTags = new()
        {
            { AppsTelemetry.Tags.HandlerType, handlerType },
            { AppsTelemetry.Tags.HandlerDispatch, dispatch },
        };

        AppsTelemetry.HandlerDispatched.Add(1, handlerTags);

        using Activity? span = AppsTelemetry.Source.StartActivity(AppsTelemetry.Spans.Handler, ActivityKind.Internal);
        if (span is not null)
        {
            span.SetTag(AppsTelemetry.Tags.HandlerType, handlerType);
            span.SetTag(AppsTelemetry.Tags.HandlerDispatch, dispatch);
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        InvokeResponse response;
        try
        {
            response = await matchingRoutes[0].InvokeRouteWithReturn(ctx, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppsTelemetry.HandlerFailures.Add(1, handlerTags);
            span.RecordException(ex);
            throw;
        }
        finally
        {
            double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            AppsTelemetry.HandlerDuration.Record(elapsedMs, handlerTags);
        }

        _logger.LogDebug("Completed invoke route '{Route}' for '{Name}' with status {Status}.", matchingRoutes[0].Name, name, response.Status);

        return response;
    }

    private static (string handlerType, string dispatch) GetHandlerTags(string routeName)
    {
        const string invokePrefix = TeamsActivityType.Invoke + "/";
        if (string.Equals(routeName, TeamsActivityType.Invoke, StringComparison.Ordinal))
        {
            return (routeName, "catchall");
        }
        if (routeName.StartsWith(invokePrefix, StringComparison.Ordinal))
        {
            return (routeName[invokePrefix.Length..], "invoke");
        }
        return (routeName, "type");
    }
}
