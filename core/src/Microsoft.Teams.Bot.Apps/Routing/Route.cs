// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Routing;

/// <summary>
/// Base class for routes, providing non-generic access to route functionality
/// </summary>
public abstract class RouteBase
{
    /// <summary>
    /// Gets or sets the name of the route
    /// </summary>
    public abstract string Name { get; set; }

    /// <summary>
    /// Determines if the route matches the given activity
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public abstract bool Matches (TeamsActivity activity);

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task InvokeRoute(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type and returns a response
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<InvokeResponse> InvokeRouteWithReturn(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a route for handling Teams activities
/// </summary>
public class Route<TActivity> : RouteBase where TActivity : TeamsActivity
{
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the name of the route
    /// </summary>
    public override string Name
    {
        get => _name;
        set => _name = value;
    }

    /// <summary>
    /// Predicate function to determine if this route should handle the activity
    /// </summary>
    public Func<TActivity, bool> Selector { get; set; } = _ => true;

    /// <summary>
    /// Handler function to process the activity
    /// </summary>
    public Func<Context<TActivity>, CancellationToken, Task>? Handler { get; set; }

    /// <summary>
    /// Handler function to process the activity and return a response
    /// </summary>
    public Func<Context<TActivity>, CancellationToken, Task<InvokeResponse>>? HandlerWithReturn { get; set; }

    /// <summary>
    /// Determines if the route matches the given activity
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public override bool Matches(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity is TActivity && Selector((TActivity)activity);
    }

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task InvokeRoute(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.Activity is TActivity typedActivity)
        {
            Context<TActivity> typedContext = new(ctx.TeamsBotApplication, typedActivity);
            if (Handler is not null)
            {
                await Handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Invokes the route handler if the activity matches the expected type and returns a response
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override async Task<InvokeResponse> InvokeRouteWithReturn(Context<TeamsActivity> ctx, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.Activity is TActivity typedActivity)
        {
            Context<TActivity> typedContext = new(ctx.TeamsBotApplication, typedActivity);
            if (HandlerWithReturn is not null)
            {
                return await HandlerWithReturn(typedContext, cancellationToken).ConfigureAwait(false);
            }
        }
        return null!; // TODO: throw?
    }
}
