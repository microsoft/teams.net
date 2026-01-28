// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling message update activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageUpdateHandler(Context<MessageUpdateActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message update activity handlers.
/// </summary>
public static class MessageUpdateExtensions
{
    /// <summary>
    /// Registers a handler for message update activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessageUpdate(this TeamsBotApplication app, MessageUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageUpdateActivity>
        {
            Name = TeamsActivityType.MessageUpdate,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
