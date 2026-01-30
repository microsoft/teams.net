// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling conversation update activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task ConversationUpdateHandler(Context<ConversationUpdateActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering conversation update activity handlers.
/// </summary>
public static class ConversationUpdateExtensions
{
    /// <summary>
    /// Registers a handler for conversation update activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnConversationUpdate(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for conversation update activities where members were added.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMembersAdded(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.MembersAdded?.Count > 0,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for conversation update activities where members were removed.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMembersRemoved(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.MembersRemoved?.Count > 0,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    // Channel Event Handlers

    /// <summary>
    /// Registers a handler for channel created events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelCreated(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelCreated,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel deleted events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelDeleted(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelDeleted,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel renamed events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelRenamed(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelRenamed,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel restored events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelRestored(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelRestored,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel shared events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelShared(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelShared,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel unshared events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelUnshared(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelUnShared,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel member added events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelMemberAdded(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelMemberAdded,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for channel member removed events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnChannelMemberRemoved(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.ChannelMemberRemoved,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    // Team Event Handlers

    /// <summary>
    /// Registers a handler for team member added events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamMemberAdded(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamMemberAdded,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team member removed events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamMemberRemoved(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamMemberRemoved,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team archived events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamArchived(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamArchived,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team deleted events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamDeleted(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamDeleted,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team hard deleted events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamHardDeleted(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamHardDeleted,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team renamed events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamRenamed(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamRenamed,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team restored events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamRestored(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamRestored,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for team unarchived events.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnTeamUnarchived(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityType.ConversationUpdate,
            Selector = activity => activity.ChannelData?.EventType == ConversationEventTypes.TeamUnarchived,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
