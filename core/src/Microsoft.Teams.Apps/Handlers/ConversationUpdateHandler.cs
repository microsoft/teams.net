// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Delegate for handling conversation update activities.
/// </summary>
/// <param name="context">The context for the conversation update activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task ConversationUpdateHandler(Context<ConversationUpdateActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering conversation update activity handlers.
/// </summary>
public static class ConversationUpdateExtensions
{
    /// <summary>
    /// Registers a handler for conversation update activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnConversationUpdate(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = TeamsActivityTypes.ConversationUpdate,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMembersAdded(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, "membersAdded"]),
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMembersRemoved(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, "membersRemoved"]),
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelCreated(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelCreated]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelCreated) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelDeleted(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelDeleted]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelDeleted) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelRenamed(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelRenamed]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelRenamed) ?? false,
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
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelShared(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelShared]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelShared) ?? false,
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
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelUnshared(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelUnShared]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelUnShared) ?? false,
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
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelMemberAdded(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelMemberAdded]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelMemberAdded) ?? false,
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
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelMemberRemoved(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelMemberRemoved]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelMemberRemoved) ?? false,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /*
    /// <summary>
    /// Registers a handler for channel restored events.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnChannelRestored(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.ChannelRestored]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.ChannelRestored) ?? false,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
    */

    // Team Event Handlers

    /// <summary>
    /// Registers a handler for team member added events.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamMemberAdded(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamMemberAdded]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamMemberAdded) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamMemberRemoved(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamMemberRemoved]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamMemberRemoved) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamArchived(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamArchived]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamArchived) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamDeleted(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamDeleted]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamDeleted) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamRenamed(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamRenamed]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamRenamed) ?? false,
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamUnarchived(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamUnarchived]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamUnarchived) ?? false,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /*
    /// Registers a handler for team restored events.
    /// </summary>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamRestored(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamRestored]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamRestored) ?? false,
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
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnTeamHardDeleted(this TeamsBotApplication app, ConversationUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<ConversationUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.ConversationUpdate, ConversationEventTypes.TeamHardDeleted]),
            Selector = activity => activity.ChannelData?.EventType?.Equals(ConversationEventTypes.TeamHardDeleted) ?? false,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
    */
}
