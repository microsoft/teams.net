// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Delegate for handling meeting start event activities.
/// </summary>
/// <param name="context">The context for the event activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MeetingStartHandler(Context<EventActivity<MeetingStartValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling meeting end event activities.
/// </summary>
/// <param name="context">The context for the event activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MeetingEndHandler(Context<EventActivity<MeetingEndValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling meeting participant join event activities.
/// </summary>
/// <param name="context">The context for the event activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MeetingParticipantJoinHandler(Context<EventActivity<MeetingParticipantJoinValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling meeting participant leave event activities.
/// </summary>
/// <param name="context">The context for the event activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MeetingParticipantLeaveHandler(Context<EventActivity<MeetingParticipantLeaveValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering meeting event activity handlers.
/// </summary>
public static class MeetingExtensions
{
    /// <summary>
    /// Registers a handler for meeting start event activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMeetingStart(this TeamsBotApplication app, MeetingStartHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Event, EventNames.MeetingStart),
            Selector = activity => activity.Name == EventNames.MeetingStart,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingStartValue> typedActivity = new(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for meeting end event activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMeetingEnd(this TeamsBotApplication app, MeetingEndHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Event, EventNames.MeetingEnd),
            Selector = activity => activity.Name == EventNames.MeetingEnd,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingEndValue> typedActivity = new(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for meeting participant join event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMeetingJoin(this TeamsBotApplication app, MeetingParticipantJoinHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Event, EventNames.MeetingParticipantJoin),
            Selector = activity => activity.Name == EventNames.MeetingParticipantJoin,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingParticipantJoinValue> typedActivity = new(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for meeting participant leave event activities.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMeetingLeave(this TeamsBotApplication app, MeetingParticipantLeaveHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityTypes.Event, EventNames.MeetingParticipantLeave),
            Selector = activity => activity.Name == EventNames.MeetingParticipantLeave,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingParticipantLeaveValue> typedActivity = new(ctx.Activity);
                var typedContext = ctx.CreateDerivedContext(typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
