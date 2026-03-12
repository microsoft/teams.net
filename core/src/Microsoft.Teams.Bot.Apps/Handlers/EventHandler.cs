// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling any event activity.
/// </summary>
/// <param name="context">The context for the event activity, providing access to the activity data and bot application.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task EventActivityHandler(Context<EventActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering generic event activity handlers.
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Registers a handler for all event activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnEvent(this TeamsBotApplication app, EventActivityHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = TeamsActivityType.Event,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /*
    /// <summary>
    /// Registers a handler for read receipt event activities.
    /// Fired by Teams when a user reads a message sent by the bot in a 1:1 chat.
    /// No value payload — the event itself is the notification.
    /// </summary>
    public static TeamsBotApplication OnReadReceipt(this TeamsBotApplication app, EventActivityHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<EventActivity>
        {
            Name = string.Join("/", TeamsActivityType.Event, EventNames.ReadReceipt),
            Selector = activity => activity.Name == EventNames.ReadReceipt,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
    */
}
