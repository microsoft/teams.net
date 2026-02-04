// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Agents.Hosting.BotCore;

/// <summary>
/// Provides extension methods for registering compatibility adapters and related services
/// to support Microsoft.Agents framework with Microsoft.Teams.Bot.Core.
/// </summary>
/// <remarks>
/// These extension methods simplify the integration of the BotCore compatibility layer
/// into Microsoft.Agents applications by adding required services to the dependency injection container.
/// </remarks>
public static class CompatHostingExtensions
{
    /// <summary>
    /// Adds the BotCore compatibility layer services to the application's dependency injection container
    /// and registers the specified agent.
    /// </summary>
    /// <typeparam name="TAgent">The type of agent to register. Must implement <see cref="IAgent"/>.</typeparam>
    /// <param name="builder">The host application builder to which the services will be added.</param>
    /// <param name="sectionName">The configuration section name for Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The same <paramref name="builder"/> instance, enabling method chaining.</returns>
    public static IHostApplicationBuilder AddAgentWithBotCore<TAgent>(
        this IHostApplicationBuilder builder,
        string sectionName = "AzureAd") where TAgent : class, IAgent
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddAgentWithBotCore<TAgent>(sectionName);
        return builder;
    }

    /// <summary>
    /// Adds the BotCore compatibility layer services to the service collection
    /// and registers the specified agent.
    /// </summary>
    /// <typeparam name="TAgent">The type of agent to register. Must implement <see cref="IAgent"/>.</typeparam>
    /// <param name="services">The service collection to which the services will be added.</param>
    /// <param name="sectionName">The configuration section name for Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The same <paramref name="services"/> instance, enabling method chaining.</returns>
    public static IServiceCollection AddAgentWithBotCore<TAgent>(
        this IServiceCollection services,
        string sectionName = "AzureAd") where TAgent : class, IAgent
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Bot.Core services (BotApplication, ConversationClient, UserTokenClient, auth)
        services.AddBotApplication<BotApplication>(sectionName);

        // Required for invoke response handling
        services.AddHttpContextAccessor();

        // Register the compatibility adapters
        services.AddSingleton<CompatChannelAdapter>();
        services.AddSingleton<IAgentHttpAdapter, CompatAgentAdapter>();

        // Register the Agent
        services.AddSingleton<IAgent, TAgent>();

        return services;
    }

    /// <summary>
    /// Adds only the BotCore compatibility layer services without registering an agent.
    /// Use this when you want to register the agent separately or use a different registration pattern.
    /// </summary>
    /// <param name="services">The service collection to which the services will be added.</param>
    /// <param name="sectionName">The configuration section name for Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The same <paramref name="services"/> instance, enabling method chaining.</returns>
    public static IServiceCollection AddBotCoreCompatibility(
        this IServiceCollection services,
        string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Bot.Core services
        services.AddBotApplication<BotApplication>(sectionName);

        // Required for invoke response handling
        services.AddHttpContextAccessor();

        // Register the compatibility adapters
        services.AddSingleton<CompatChannelAdapter>();
        services.AddSingleton<IAgentHttpAdapter, CompatAgentAdapter>();

        return services;
    }

    /// <summary>
    /// Adds only the BotCore compatibility layer services without registering an agent.
    /// Use this when you want to register the agent separately or use a different registration pattern.
    /// </summary>
    /// <param name="builder">The host application builder to which the services will be added.</param>
    /// <param name="sectionName">The configuration section name for Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The same <paramref name="builder"/> instance, enabling method chaining.</returns>
    public static IHostApplicationBuilder AddBotCoreCompatibility(
        this IHostApplicationBuilder builder,
        string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddBotCoreCompatibility(sectionName);
        return builder;
    }
}
