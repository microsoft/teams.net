// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling message delete activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageDeleteHandler(Context<MessageDeleteActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message delete activity handlers.
/// </summary>
public static class MessageDeleteExtensions
{
    /// <summary>
    /// Registers a handler for message delete activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessageDelete(this TeamsBotApplication app, MessageDeleteHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageDeleteActivity>
        {
            Name = TeamsActivityType.MessageDelete,
            Selector = _ => true,
            Handler = (ctx, cancellationToken) => handler(ctx, cancellationToken)
        });

        return app;
    }
}
