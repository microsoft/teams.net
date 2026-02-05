// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if false

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling end of conversation activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task EndOfConversationHandler(Context<EndOfConversationActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering end of conversation activity handlers.
/// </summary>
public static class EndOfConversationExtensions
{
    /// <summary>
    /// Registers a handler for end of conversation activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnEndOfConversation(this TeamsBotApplication app, EndOfConversationHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EndOfConversationActivity>
        {
            Name = TeamsActivityType.EndOfConversation,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
#endif
