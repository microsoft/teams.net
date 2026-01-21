// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling message activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task MessageHandler(Context<MessageActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering message activity handlers.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Registers a handler for message activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessage(this TeamsBotApplication app, MessageHandler handler)
    {
        TeamsBotApplication.Router.Register(new Route<MessageActivity>
        {

            Name = ActivityType.Message,
            Selector = _ => true,
            Handler = async ctx =>
            {
                await handler(ctx).ConfigureAwait(false);
                return null;
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message activities matching the specified pattern.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="pattern"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessage(this TeamsBotApplication app, string pattern, MessageHandler handler)
    {
        var regex = new Regex(pattern);

        TeamsBotApplication.Router.Register(new Route<MessageActivity>
        {
            Name = ActivityType.Message,
            Selector = msg => regex.IsMatch(msg.Text ?? ""),
            Handler = async ctx =>
            {
                await handler(ctx).ConfigureAwait(false);
                return null;
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for message activities matching the specified regex.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="regex"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnMessage(this TeamsBotApplication app, Regex regex, MessageHandler handler)
    {
        TeamsBotApplication.Router.Register(new Route<MessageActivity>
        {
            Name = ActivityType.Message,
            Selector = msg => regex.IsMatch(msg.Text ?? ""),
            Handler = async ctx =>
            {
                await handler(ctx).ConfigureAwait(false);
                return null;
            }
        });

        return app;
    }
}

