// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Handlers.TaskModules;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling message fetch task invoke activities.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
public delegate Task<InvokeResponse<TaskModuleResponse>> MessageFetchTaskHandler(Context<InvokeActivity<MessageFetchTaskInvokeValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message fetch task invoke handlers.
/// </summary>
public static class MessageFetchTaskExtensions
{
    /// <summary>
    /// Registers a handler for message fetch task invoke activities (message/fetchTask).
    /// Triggered when the user clicks a feedback button on an AI-generated message.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessageFetchTask(this TeamsBotApplication app, MessageFetchTaskHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageFetchTask),
            Selector = activity => activity.Name == InvokeNames.MessageFetchTask,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<MessageFetchTaskInvokeValue> typedActivity = new(ctx.Activity);
                Context<InvokeActivity<MessageFetchTaskInvokeValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
