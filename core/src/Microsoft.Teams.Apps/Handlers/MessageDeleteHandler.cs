// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Delegate for handling message delete activities.
/// </summary>
/// <param name="context">The context for the message delete activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MessageDeleteHandler(Context<MessageDeleteActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message delete activity handlers.
/// </summary>
public static class MessageDeleteExtensions
{
    /// <summary>
    /// Registers a handler for message delete activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessageDelete(this TeamsBotApplication app, MessageDeleteHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageDeleteActivity>
        {
            Name = TeamsActivityTypes.MessageDelete,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
