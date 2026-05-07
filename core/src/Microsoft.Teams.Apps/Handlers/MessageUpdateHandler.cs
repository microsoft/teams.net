// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling message update activities.
/// </summary>
/// <param name="context">The context for the message update activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MessageUpdateHandler(Context<MessageUpdateActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message update activity handlers.
/// </summary>
public static class MessageUpdateExtensions
{
    /// <summary>
    /// Registers a handler for message update activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
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
