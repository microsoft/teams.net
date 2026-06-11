// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling message submit action invoke activities.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
public delegate Task<InvokeResponse> MessageSubmitActionHandler(Context<InvokeActivity<SubmitActionValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling <c>message/submitAction</c> invokes whose <c>actionName</c> is <c>"feedback"</c>.
/// The activity's <c>Value</c> is the typed inner <see cref="MessageSubmitFeedbackValue"/>.
/// </summary>
/// <param name="context">The context for the invoke activity, providing access to the typed feedback value and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task that represents the asynchronous operation. The task result contains the invoke response.</returns>
public delegate Task<InvokeResponse> MessageSubmitFeedbackHandler(Context<InvokeActivity<MessageSubmitFeedbackValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message submit action invoke handlers.
/// </summary>
public static class MessageSubmitActionExtensions
{
    /// <summary>
    /// Registers a handler for message submit action invoke activities.
    /// Cannot be combined with <see cref="InvokeExtensions.OnInvoke"/>.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessageSubmitAction(this TeamsBotApplication app, MessageSubmitActionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageSubmitAction),
            Selector = activity => activity.Name == InvokeNames.MessageSubmitAction,
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<SubmitActionValue> typedActivity = new(ctx.Activity);
                Context<InvokeActivity<SubmitActionValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                if (ctx.HasState) typedContext.State = ctx.State;
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for <c>message/submitAction</c> invokes where <c>actionName == "feedback"</c>.
    /// The handler receives the inner <c>actionValue</c> typed as <see cref="MessageSubmitFeedbackValue"/>.
    /// Register this before any general <see cref="OnMessageSubmitAction"/> so the feedback-specific route
    /// wins on first-match dispatch.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessageSubmitFeedback(this TeamsBotApplication app, MessageSubmitFeedbackHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InvokeActivity>
        {
            Name = string.Join("/", TeamsActivityType.Invoke, InvokeNames.MessageSubmitAction, "feedback"),
            Selector = activity =>
                activity.Name == InvokeNames.MessageSubmitAction
                && activity.Value?["actionName"]?.GetValue<string>() == "feedback",
            HandlerWithReturn = async (ctx, cancellationToken) =>
            {
                InvokeActivity<MessageSubmitFeedbackValue> typedActivity = new(ctx.Activity);
                ((InvokeActivity)typedActivity).Value = ctx.Activity.Value?["actionValue"];

                Context<InvokeActivity<MessageSubmitFeedbackValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                if (ctx.HasState) typedContext.State = ctx.State;
                return await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
