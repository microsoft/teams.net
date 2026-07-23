// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Delegate for handling all Agent 365 lifecycle event activity variants.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgentLifecycleHandler(Context<AgentLifecycleEventActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user identity created lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserIdentityCreatedHandler(Context<AgentLifecycleEventActivity<AgenticUserIdentityCreatedValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user identity updated lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserIdentityUpdatedHandler(Context<AgentLifecycleEventActivity<AgenticUserIdentityUpdatedValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user manager updated lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserManagerUpdatedHandler(Context<AgentLifecycleEventActivity<AgenticUserManagerUpdatedValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user enabled lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserEnabledHandler(Context<AgentLifecycleEventActivity<AgenticUserEnabledValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user disabled lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserDisabledHandler(Context<AgentLifecycleEventActivity<AgenticUserDisabledValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user deleted lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserDeletedHandler(Context<AgentLifecycleEventActivity<AgenticUserDeletedValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user undeleted lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserUndeletedHandler(Context<AgentLifecycleEventActivity<AgenticUserUndeletedValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling agentic user workload onboarding updated lifecycle event activities.
/// </summary>
/// <param name="context">The context for the lifecycle event activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task AgenticUserWorkloadOnboardingUpdatedHandler(Context<AgentLifecycleEventActivity<AgenticUserWorkloadOnboardingUpdatedValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering Agent 365 lifecycle event activity handlers.
/// </summary>
public static class AgentLifecycleExtensions
{
    /// <summary>
    /// Registers a handler for all Agent 365 <c>agentLifecycle</c> event activity variants.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgentLifecycle(this TeamsBotApplication app, AgentLifecycleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            EventNames.AgentLifecycle,
            valueType: null,
            activity => new AgentLifecycleEventActivity(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserIdentityCreated</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserIdentityCreated(this TeamsBotApplication app, AgenticUserIdentityCreatedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserIdentityCreated,
            activity => new AgentLifecycleEventActivity<AgenticUserIdentityCreatedValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserIdentityUpdated</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserIdentityUpdated(this TeamsBotApplication app, AgenticUserIdentityUpdatedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserIdentityUpdated,
            activity => new AgentLifecycleEventActivity<AgenticUserIdentityUpdatedValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserManagerUpdated</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserManagerUpdated(this TeamsBotApplication app, AgenticUserManagerUpdatedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserManagerUpdated,
            activity => new AgentLifecycleEventActivity<AgenticUserManagerUpdatedValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserEnabled</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserEnabled(this TeamsBotApplication app, AgenticUserEnabledHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserEnabled,
            activity => new AgentLifecycleEventActivity<AgenticUserEnabledValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserDisabled</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserDisabled(this TeamsBotApplication app, AgenticUserDisabledHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserDisabled,
            activity => new AgentLifecycleEventActivity<AgenticUserDisabledValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserDeleted</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserDeleted(this TeamsBotApplication app, AgenticUserDeletedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserDeleted,
            activity => new AgentLifecycleEventActivity<AgenticUserDeletedValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserUndeleted</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserUndeleted(this TeamsBotApplication app, AgenticUserUndeletedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserUndeleted,
            activity => new AgentLifecycleEventActivity<AgenticUserUndeletedValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    /// <summary>
    /// Registers a handler for <c>AgenticUserWorkloadOnboardingUpdated</c> lifecycle event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnAgenticUserWorkloadOnboardingUpdated(this TeamsBotApplication app, AgenticUserWorkloadOnboardingUpdatedHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));
        return app.RegisterAgentLifecycleRoute(
            AgentLifecycleEventValueTypes.AgenticUserWorkloadOnboardingUpdated,
            activity => new AgentLifecycleEventActivity<AgenticUserWorkloadOnboardingUpdatedValue>(activity),
            (ctx, cancellationToken) => handler(ctx, cancellationToken));
    }

    private static TeamsBotApplication RegisterAgentLifecycleRoute<TActivity>(
        this TeamsBotApplication app,
        string valueType,
        Func<EventActivity, TActivity> createActivity,
        Func<Context<TActivity>, CancellationToken, Task> handler) where TActivity : AgentLifecycleEventActivity
    {
        return app.RegisterAgentLifecycleRoute(valueType, valueType, createActivity, handler);
    }

    private static TeamsBotApplication RegisterAgentLifecycleRoute<TActivity>(
        this TeamsBotApplication app,
        string routeSuffix,
        string? valueType,
        Func<EventActivity, TActivity> createActivity,
        Func<Context<TActivity>, CancellationToken, Task> handler) where TActivity : AgentLifecycleEventActivity
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        ArgumentException.ThrowIfNullOrWhiteSpace(routeSuffix);
        ArgumentNullException.ThrowIfNull(createActivity, nameof(createActivity));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        string routeName = valueType is null
            ? string.Join("/", TeamsActivityTypes.Event, routeSuffix)
            : string.Join("/", TeamsActivityTypes.Event, EventNames.AgentLifecycle, routeSuffix);

        app.Router.Register(new Route<EventActivity>
        {
            Name = routeName,
            Selector = activity =>
                activity.Name == EventNames.AgentLifecycle
                && (valueType is null || activity.Properties.Get<string>("valueType") == valueType),
            Handler = async (ctx, cancellationToken) =>
            {
                TActivity typedActivity = createActivity(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
