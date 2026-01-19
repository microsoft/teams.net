// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Provides extension methods for registering compatibility adapters and related services to support legacy bot hosting
/// scenarios.
/// </summary>
/// <remarks>These extension methods simplify the integration of compatibility adapters into modern hosting
/// environments by adding required services to the dependency injection container. Use these methods to enable legacy
/// bot functionality within applications built on the current hosting model.</remarks>
public static class CompatHostingExtensions
{
    /// <summary>
    /// Adds compatibility adapter services to the application's dependency injection container.
    /// </summary>
    /// <remarks>This method registers services required for compatibility scenarios. It can be called
    /// multiple times without adverse effects.</remarks>
    /// <param name="builder">The host application builder to which the compatibility adapter services will be added. Cannot be null.</param>
    /// <returns>The same <paramref name="builder"/> instance, enabling method chaining.</returns>
    public static IHostApplicationBuilder AddCompatAdapter(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddCompatAdapter();
        return builder;
    }

    /// <summary>
    /// Registers the compatibility bot adapter and related services required for Bot Framework HTTP integration with
    /// the application's dependency injection container.
    /// </summary>
    /// <remarks>Call this method during application startup to enable Bot Framework HTTP endpoint support
    /// using the compatibility adapter. This method should be invoked before building the service provider.</remarks>
    /// <param name="services">The service collection to which the compatibility adapter and related services will be added. Must not be null.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance provided in <paramref name="services"/>, with the
    /// compatibility adapter and related services registered.</returns>
    public static IServiceCollection AddCompatAdapter(this IServiceCollection services)
    {
        services.AddTeamsBotApplication();
        services.AddSingleton<CompatBotAdapter>();
        services.AddSingleton<IBotFrameworkHttpAdapter, CompatAdapter>();
        return services;
    }

    /// <summary>
    /// Adds Bot Framework v4 middleware to the bot application's middleware pipeline by wrapping it in a compatibility adapter.
    /// </summary>
    /// <param name="app">The bot application to configure.</param>
    /// <param name="bfMiddleware">The Bot Framework v4 middleware to wrap. Cannot be null.</param>
    /// <returns>The <see cref="BotApplication"/> instance for method chaining.</returns>
    /// <remarks>
    /// This method enables gradual migration from Bot Framework v4 by allowing existing v4 middleware
    /// to run in the new SDK's middleware pipeline. The middleware is wrapped in a compatibility adapter
    /// that translates between the new SDK's activity format and Bot Framework v4's activity format.
    /// <example>
    /// Wrap existing Bot Framework v4 middleware:
    /// <code>
    /// // Existing BF v4 middleware
    /// var showTypingMiddleware = new ShowTypingMiddleware();
    ///
    /// // Wrap for use in new SDK
    /// botApp.UseCompatMiddleware(showTypingMiddleware);
    /// </code>
    /// </example>
    /// </remarks>
    public static BotApplication UseCompatMiddleware(
        this BotApplication app,
        IMiddleware bfMiddleware)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(bfMiddleware);

        app.Use(new CompatAdapterMiddleware(bfMiddleware));
        return app;
    }
}
