// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling installation update activities.
/// </summary>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task InstallUpdateHandler(Context<InstallUpdateActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering installation update activity handlers.
/// </summary>
public static class InstallUpdateExtensions
{
    /// <summary>
    /// Registers a handler for installation update activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnInstallUpdate(this TeamsBotApplication app, InstallUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InstallUpdateActivity>
        {
            Name = TeamsActivityType.InstallationUpdate,
            Selector = _ => true,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for installation add activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnInstall(this TeamsBotApplication app, InstallUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InstallUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityType.InstallationUpdate, InstallUpdateActions.Add]),
            Selector = activity => activity.Action == InstallUpdateActions.Add,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for installation remove activities.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static TeamsBotApplication OnUnInstall(this TeamsBotApplication app, InstallUpdateHandler handler)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        app.Router.Register(new Route<InstallUpdateActivity>
        {
            Name = string.Join("/", [TeamsActivityType.InstallationUpdate, InstallUpdateActions.Remove]),
            Selector = activity => activity.Action == InstallUpdateActions.Remove,
            Handler = async (ctx, cancellationToken) =>
            {
                await handler(ctx, cancellationToken).ConfigureAwait(false);
            }
        });

        return app;
    }
}
