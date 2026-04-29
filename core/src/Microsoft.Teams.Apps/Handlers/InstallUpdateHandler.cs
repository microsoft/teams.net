// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Delegate for handling installation update activities.
/// </summary>
/// <param name="context">The context for the installation update activity.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task InstallUpdateHandler(Context<InstallUpdateActivity> context, CancellationToken cancellationToken = default);

/// <summary>
/// Extension methods for registering installation update activity handlers.
/// </summary>
public static class InstallUpdateExtensions
{
    /// <summary>
    /// Registers a handler for installation update activities.
    /// </summary>
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
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
    /// <remarks>
    /// Breaking change: previously only the first matching handler was invoked. All matching handlers are now invoked sequentially.
    /// </remarks>
    /// <param name="app">The Teams bot application.</param>
    /// <param name="handler">The handler to register.</param>
    /// <returns>The updated Teams bot application.</returns>
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
