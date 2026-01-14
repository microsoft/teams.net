// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.BotApps;

namespace Microsoft.Bot.Core.Compat;

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
}
