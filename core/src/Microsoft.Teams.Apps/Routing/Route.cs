// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Routing;

/// <summary>
/// Base class for routes, providing non-generic access to route functionality
/// </summary>
internal abstract class RouteBase
{
    /// <summary>
    /// Gets or sets the name of the route
    /// </summary>
    internal abstract string Name { get; set; }

    /// <summary>
    /// Determines if the route matches the given activity
    /// </summary>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if the route matches the activity; otherwise, false.</returns>
    internal abstract bool Matches(TeamsActivity activity);

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type
    /// </summary>
    /// <param name="ctx">The activity context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal abstract Task InvokeRoute(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type and returns a response
    /// </summary>
    /// <param name="ctx">The activity context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal abstract Task<InvokeResponse> InvokeRouteWithReturn(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a route for handling Teams activities
/// </summary>
internal class Route<TActivity> : RouteBase where TActivity : TeamsActivity
{
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name of the route
    /// </summary>
    internal override string Name
    {
        get => _name;
        set => _name = value;
    }

    /// <summary>
    /// Predicate function to determine if this route should handle the activity
    /// </summary>
    internal Func<TActivity, bool> Selector { get; set; } = _ => true;

    /// <summary>
    /// Handler function to process the activity
    /// </summary>
    internal Func<Context<TActivity>, CancellationToken, Task>? Handler { get; set; }

    /// <summary>
    /// Handler function to process the activity and return a response
    /// </summary>
    internal Func<Context<TActivity>, CancellationToken, Task<InvokeResponse>>? HandlerWithReturn { get; set; }

    /// <summary>
    /// Determines if the route matches the given activity
    /// </summary>
    /// <param name="activity">The activity to check.</param>
    /// <returns>True if the route matches the activity; otherwise, false.</returns>
    internal override bool Matches(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity is TActivity activity1 && Selector(activity1);
    }

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type
    /// </summary>
    /// <param name="ctx">The activity context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal override async Task InvokeRoute(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var typedContext = ctx.CreateDerivedContext((TActivity)ctx.Activity);
        await Handler!(typedContext, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type and returns a response
    /// </summary>
    /// <param name="ctx">The activity context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal override async Task<InvokeResponse> InvokeRouteWithReturn(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var typedContext = ctx.CreateDerivedContext((TActivity)ctx.Activity);
        return await HandlerWithReturn!(typedContext, cancellationToken).ConfigureAwait(false);
    }
}
