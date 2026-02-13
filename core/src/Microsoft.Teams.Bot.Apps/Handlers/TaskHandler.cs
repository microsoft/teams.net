// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Invokes;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling task module invoke activities with strongly-typed value and response.
/// </summary>
public delegate Task<InvokeResponse<TaskModuleResponse>> TaskModuleHandler(Context<InvokeActivity<TaskModuleRequest>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering task module invoke handlers.
/// </summary>
public static class TaskExtensions
{

    /// <summary>
    /// Registers a handler for task module fetch invoke activities with strongly-typed value and response.
    /// </summary>
    public static TeamsBotApplication OnTaskFetch(this TeamsBotApplication app, TaskModuleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.TaskFetch),
            Selector = activity => activity.Name == InvokeNames.TaskFetch,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<TaskModuleRequest> typedActivity = new (ctx.Activity);
                Context<InvokeActivity<TaskModuleRequest>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                InvokeResponse<TaskModuleResponse> typedResponse = await handler(typedContext, cancellationToken).ConfigureAwait(false);
                return typedResponse;
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for task module submit invoke activities with strongly-typed value and response.
    /// </summary>
    public static TeamsBotApplication OnTaskSubmit(this TeamsBotApplication app, TaskModuleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.TaskSubmit),
            Selector = activity => activity.Name == InvokeNames.TaskSubmit,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<TaskModuleRequest> typedActivity = new (ctx.Activity);
                Context<InvokeActivity<TaskModuleRequest>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                InvokeResponse<TaskModuleResponse> typedResponse = await handler(typedContext, cancellationToken).ConfigureAwait(false);
                return typedResponse;
            }
        });

        return app;
    }
}
