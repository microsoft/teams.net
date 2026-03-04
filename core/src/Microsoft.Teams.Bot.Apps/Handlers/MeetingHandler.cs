// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling meeting start event activities.
/// </summary>
public delegate Task MeetingStartHandler(Context<EventActivity<MeetingStartValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling meeting end event activities.
/// </summary>
public delegate Task MeetingEndHandler(Context<EventActivity<MeetingEndValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling meeting participant join event activities.
/// </summary>
public delegate Task MeetingParticipantJoinHandler(Context<EventActivity<MeetingParticipantJoinValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Delegate for handling meeting participant leave event activities.
/// </summary>
public delegate Task MeetingParticipantLeaveHandler(Context<EventActivity<MeetingParticipantLeaveValue>> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering meeting event activity handlers.
/// </summary>
public static class MeetingExtensions
{
    /// <summary>
    /// Registers a handler for meeting start event activities.
    /// </summary>
    public static TeamsBotApplication OnMeetingStart(this TeamsBotApplication app, MeetingStartHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityType.Event, EventNames.MeetingStart),
            Selector = activity => activity.Name == EventNames.MeetingStart,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingStartValue> typedActivity = new(ctx.Activity);
                Context<EventActivity<MeetingStartValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for meeting end event activities.
    /// </summary>
    public static TeamsBotApplication OnMeetingEnd(this TeamsBotApplication app, MeetingEndHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityType.Event, EventNames.MeetingEnd),
            Selector = activity => activity.Name == EventNames.MeetingEnd,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingEndValue> typedActivity = new(ctx.Activity);
                Context<EventActivity<MeetingEndValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for meeting participant join event activities.
    /// </summary>
    public static TeamsBotApplication OnMeetingParticipantJoin(this TeamsBotApplication app, MeetingParticipantJoinHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityType.Event, EventNames.MeetingParticipantJoin),
            Selector = activity => activity.Name == EventNames.MeetingParticipantJoin,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingParticipantJoinValue> typedActivity = new(ctx.Activity);
                Context<EventActivity<MeetingParticipantJoinValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for meeting participant leave event activities.
    /// </summary>
    public static TeamsBotApplication OnMeetingParticipantLeave(this TeamsBotApplication app, MeetingParticipantLeaveHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityType.Event, EventNames.MeetingParticipantLeave),
            Selector = activity => activity.Name == EventNames.MeetingParticipantLeave,
            Handler = async (ctx, cancellationToken) =>
            {
                EventActivity<MeetingParticipantLeaveValue> typedActivity = new(ctx.Activity);
                Context<EventActivity<MeetingParticipantLeaveValue>> typedContext = new(ctx.TeamsBotApplication, typedActivity);
                await handler(typedContext, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
