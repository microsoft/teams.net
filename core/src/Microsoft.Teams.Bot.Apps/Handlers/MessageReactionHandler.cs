// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling message reaction activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageReactionHandler(Context<MessageReactionActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message reaction activity handlers.
/// </summary>
public static class MessageReactionExtensions
{
    /// <summary>
    /// Registers a handler for message reaction activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessageReaction(this TeamsBotApplication app, MessageReactionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageReactionActivity>
        {
            Name = TeamsActivityType.MessageReaction,
            Selector = _ => true,
            Handler = (ctx, cancellationToken) => handler(ctx, cancellationToken)
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message reaction activities where reactions were added.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessageReactionAdded(this TeamsBotApplication app, MessageReactionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageReactionActivity>
        {
            Name = TeamsActivityType.MessageReaction,
            Selector = activity => activity.ReactionsAdded?.Count > 0,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message reaction activities where reactions were removed.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessageReactionRemoved(this TeamsBotApplication app, MessageReactionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageReactionActivity>
        {
            Name = TeamsActivityType.MessageReaction,
            Selector = activity => activity.ReactionsRemoved?.Count > 0,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
