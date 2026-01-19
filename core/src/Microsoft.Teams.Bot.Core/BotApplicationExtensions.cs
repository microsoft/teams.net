// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Teams.Bot.Core;

/// <summary>
/// Provides extension methods for configuring middleware in the bot application pipeline.
/// </summary>
public static class BotApplicationExtensions
{
    /// <summary>
    /// Adds middleware of type <typeparamref name="TMiddleware"/> to the bot application's middleware pipeline.
    /// The middleware is resolved from the provided service provider.
    /// </summary>
    /// <typeparam name="TMiddleware">The type of middleware to add. Must implement <see cref="ITurnMiddleWare"/>.</typeparam>
    /// <param name="app">The bot application to configure.</param>
    /// <param name="serviceProvider">The service provider used to resolve the middleware instance.</param>
    /// <returns>The <see cref="BotApplication"/> instance for method chaining.</returns>
    /// <remarks>
    /// This method resolves the middleware instance from the provided service provider.
    /// The middleware's lifetime (singleton, scoped, or transient) is determined by how it was registered
    /// in the dependency injection container. Ensure the middleware type is registered in the service collection
    /// before calling this method.
    /// <example>
    /// Register middleware with dependencies:
    /// <code>
    /// services.AddTransient&lt;LoggingMiddleware&gt;();
    ///
    /// var botApp = webApp.UseBotApplication&lt;BotApplication&gt;();
    /// botApp.UseMiddleware&lt;LoggingMiddleware&gt;(webApp.Services);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if the middleware type is not registered in the service collection.
    /// </exception>
    public static BotApplication UseMiddleware<TMiddleware>(
        this BotApplication app,
        IServiceProvider serviceProvider)
        where TMiddleware : ITurnMiddleWare
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        TMiddleware middleware = serviceProvider.GetRequiredService<TMiddleware>();
        app.Use(middleware);
        return app;
    }
}
