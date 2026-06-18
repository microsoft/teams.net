// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling message activities.
/// </summary>
/// <param name="context">The context for the message activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task MessageHandler(Context<MessageActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message activity handlers.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Registers a handler for message activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessage(this TeamsBotApplication app, MessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<MessageActivity>
        {

            Name = TeamsActivityTypes.Message,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message activities matching the specified pattern.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="pattern">The regex pattern to match against the message text.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessage(this TeamsBotApplication app, string pattern, MessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        Regex regex = new(pattern);

        app.Router.Register(new Route<MessageActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.Message, pattern]),
            Selector = msg => regex.IsMatch(msg.TextWithoutMentions ?? ""),
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message activities matching the specified regex.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="regex">The regex to match against the message text.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
    public static TeamsBotApplication OnMessage(this TeamsBotApplication app, Regex regex, MessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        ArgumentNullException.ThrowIfNull(regex, nameof(regex));
        app.Router.Register(new Route<MessageActivity>
        {
            Name = string.Join("/", [TeamsActivityTypes.Message, regex.ToString()]),
            Selector = msg => regex.IsMatch(msg.TextWithoutMentions ?? ""),
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}

