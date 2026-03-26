// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Handlers.TaskModules;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers.TaskModules;

/// <summary>
/// Delegate for handling task module invoke activities.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
public delegate Task<InvokeResponse<TaskModuleResponse>> TaskModuleHandler(Context<InvokeActivity<TaskModuleRequest>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering task module invoke handlers.
/// </summary>
public static class TaskExtensions
{

    /// <summary>
    /// Registers a handler for task module fetch invoke activities.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously a catch-all invoke handler could be registered alongside specific invoke handlers. This combination now throws at registration time.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTaskFetch(this TeamsBotApplication app, TaskModuleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.TaskFetch),
            Selector = activity => activity.Name == InvokeNames.TaskFetch,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<TaskModuleRequest> typedActivity = new(ctx.Activity);
                Context<InvokeActivity<TaskModuleRequest>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false); ;
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for task module submit invoke activities.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously a catch-all invoke handler could be registered alongside specific invoke handlers. This combination now throws at registration time.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTaskSubmit(this TeamsBotApplication app, TaskModuleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.TaskSubmit),
            Selector = activity => activity.Name == InvokeNames.TaskSubmit,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<TaskModuleRequest> typedActivity = new(ctx.Activity);
                Context<InvokeActivity<TaskModuleRequest>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
