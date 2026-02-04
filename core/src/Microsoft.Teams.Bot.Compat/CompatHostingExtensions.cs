// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Provides extension methods for registering compatibility adapters and related services to support legacy bot hosting
/// scenarios.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods simplify the integration of compatibility adapters into modern hosting
/// environments by adding required services to the dependency injection container.
/// </para>
/// <para>
/// For single-instance scenarios, use <see cref="AddCompatAdapter(IServiceCollection)"/>.
/// For multi-instance scenarios where multiple bot identities are handled by a single application,
/// use <see cref="AddCompatAdapter(IServiceCollection, string, Action{CompatAdapterOptions}?)"/>.
/// </para>
/// </remarks>
public static class CompatHostingExtensions
{
    /// <summary>
    /// Adds compatibility adapter services to the application's dependency injection container.
    /// </summary>
    /// <remarks>This method registers services required for compatibility scenarios. It can be called
    /// multiple times without adverse effects.</remarks>
    /// <param name="builder">The host application builder to which the compatibility adapter services will be added. Cannot be null.</param>
    /// <returns>The same <paramref name="builder"/> instance, enabling method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddCompatAdapter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTeamsBotApplication();

        // Register keyed services with default keys so CompatAdapter/CompatBotAdapter can resolve them
        // This bridges the non-keyed services to the keyed service pattern used internally
        // Note: CompatBotAdapter uses "AzureAD" and CompatAdapter uses "AzureAd" as default keys
        services.AddKeyedSingleton<TeamsBotApplication>("AzureAD", (sp, _) =>
            sp.GetRequiredService<TeamsBotApplication>());
        services.AddKeyedSingleton<TeamsBotApplication>("AzureAd", (sp, _) =>
            sp.GetRequiredService<TeamsBotApplication>());

        services.AddSingleton<CompatBotAdapter>();
        services.AddKeyedSingleton<CompatBotAdapter>("AzureAd", (sp, _) =>
            sp.GetRequiredService<CompatBotAdapter>());

        services.AddSingleton<IBotFrameworkHttpAdapter, CompatAdapter>();
        return services;
    }

    /// <summary>
    /// Registers a keyed compatibility adapter instance with the specified configuration key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method to register multiple compat adapter instances when a single application
    /// needs to handle requests for multiple bot identities. Each instance maintains isolated
    /// HTTP clients and token caches identified by the key.
    /// </para>
    /// <para>
    /// The key is also used as the default configuration section name for reading Azure AD settings.
    /// </para>
    /// <example>
    /// <code>
    /// // Register two bot instances
    /// services.AddCompatAdapter("BotOne");
    /// services.AddCompatAdapter("BotTwo", options =>
    /// {
    ///     options.Scope = "https://custom.scope/.default";
    /// });
    ///
    /// // Later, resolve keyed services
    /// var adapter = serviceProvider.GetRequiredKeyedService&lt;CompatBotAdapter&gt;("BotOne");
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="services">The service collection to add services to. Cannot be null.</param>
    /// <param name="key">The unique key identifying this adapter instance. Used for keyed service
    /// resolution and as the default configuration section name. Cannot be null or whitespace.</param>
    /// <param name="configure">Optional delegate to configure adapter options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static IServiceCollection AddCompatAdapter(
        this IServiceCollection services,
        string key,
        Action<CompatAdapterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var options = new CompatAdapterOptions { ConfigurationSectionName = key };
        configure?.Invoke(options);

        return AddCompatAdapterCore(services, key, options);
    }

    /// <summary>
    /// Adds a keyed compatibility adapter instance to the application's dependency injection container.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload allows configuration through the <see cref="IHostApplicationBuilder"/> pattern.
    /// </para>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddCompatAdapter("BotOne");
    /// builder.AddCompatAdapter("BotTwo");
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="builder">The host application builder. Cannot be null.</param>
    /// <param name="key">The unique key identifying this adapter instance. Cannot be null or whitespace.</param>
    /// <param name="configure">Optional delegate to configure adapter options.</param>
    /// <returns>The same <paramref name="builder"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static IHostApplicationBuilder AddCompatAdapter(
        this IHostApplicationBuilder builder,
        string key,
        Action<CompatAdapterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddCompatAdapter(key, configure);
        return builder;
    }

    /// <summary>
    /// Core implementation for registering a keyed compat adapter instance.
    /// </summary>
    private static IServiceCollection AddCompatAdapterCore(
        IServiceCollection services,
        string key,
        CompatAdapterOptions options)
    {
        ILogger logger = GetOrCreateLogger(services);
        IConfiguration configuration = GetConfiguration(services);

        string configSectionName = options.ConfigurationSectionName;
        var configSection = configuration.GetSection(configSectionName);
        string scope = options.Scope
            ?? configSection.GetValue<string>("Scope")
            ?? CompatAdapterOptions.DefaultScope;

        // Register shared services (idempotent - safe to call multiple times)
        RegisterSharedServices(services);

        // Configure authorization and MSAL options for this key
        services.AddAuthorization(logger, configSectionName);
        services.Configure<MicrosoftIdentityApplicationOptions>(configSectionName, configSection);

        // Register named HttpClients with auth handlers
        RegisterHttpClients(services, key, scope, options.AuthHandlerFactory);

        // Register keyed service instances
        RegisterKeyedServices(services, key);

        return services;
    }

    /// <summary>
    /// Registers shared services that are required by all compat adapter instances.
    /// These registrations are idempotent.
    /// </summary>
    private static void RegisterSharedServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTokenAcquisition(true);
        services.AddInMemoryTokenCaches();
        services.AddAgentIdentities();
        services.AddHttpContextAccessor();
    }

    /// <summary>
    /// Registers named HTTP clients with authentication handlers for the specified key.
    /// </summary>
    private static void RegisterHttpClients(
        IServiceCollection services,
        string keyName,
        string scope,
        Func<IServiceProvider, string, string, DelegatingHandler>? authHandlerFactory)
    {
        // ConversationClient
        services.AddHttpClient($"{keyName}_ConversationClient")
            .AddHttpMessageHandler(sp => CreateAuthHandler(sp, keyName, scope, authHandlerFactory));

        // UserTokenClient
        services.AddHttpClient($"{keyName}_UserTokenClient")
            .AddHttpMessageHandler(sp => CreateAuthHandler(sp, keyName, scope, authHandlerFactory));

        // TeamsApiClient
        services.AddHttpClient($"{keyName}_TeamsApiClient")
            .AddHttpMessageHandler(sp => CreateAuthHandler(sp, keyName, scope, authHandlerFactory));
    }

    /// <summary>
    /// Creates an authentication handler using either the custom factory or the default handler.
    /// </summary>
    private static DelegatingHandler CreateAuthHandler(
        IServiceProvider sp,
        string keyName,
        string scope,
        Func<IServiceProvider, string, string, DelegatingHandler>? authHandlerFactory)
    {
        if (authHandlerFactory is not null)
        {
            return authHandlerFactory(sp, keyName, scope);
        }

        // Default: use KeyedBotAuthenticationHandler with named MSAL options
        return new KeyedBotAuthenticationHandler(
            keyName,
            sp.GetRequiredService<IAuthorizationHeaderProvider>(),
            sp.GetRequiredService<ILogger<KeyedBotAuthenticationHandler>>(),
            scope,
            sp.GetService<IOptions<ManagedIdentityOptions>>());
    }

    /// <summary>
    /// Registers keyed singleton services for the specified key.
    /// </summary>
    private static void RegisterKeyedServices(IServiceCollection services, string keyName)
    {
        // ConversationClient
        services.AddKeyedSingleton<ConversationClient>(keyName, (sp, key) =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient($"{keyName}_ConversationClient");
            return new ConversationClient(httpClient, sp.GetRequiredService<ILogger<ConversationClient>>());
        });

        // UserTokenClient
        services.AddKeyedSingleton<UserTokenClient>(keyName, (sp, key) =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient($"{keyName}_UserTokenClient");
            return new UserTokenClient(
                httpClient,
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<UserTokenClient>>());
        });

        // TeamsApiClient
        services.AddKeyedSingleton<TeamsApiClient>(keyName, (sp, key) =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient($"{keyName}_TeamsApiClient");
            return new TeamsApiClient(httpClient, sp.GetRequiredService<ILogger<TeamsApiClient>>());
        });

        // TeamsBotApplication
        services.AddKeyedSingleton<TeamsBotApplication>(keyName, (sp, key) =>
        {
            return new TeamsBotApplication(
                sp.GetRequiredKeyedService<ConversationClient>(keyName),
                sp.GetRequiredKeyedService<UserTokenClient>(keyName),
                sp.GetRequiredKeyedService<TeamsApiClient>(keyName),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<ILogger<BotApplication>>(),
                keyName);
        });

        // CompatBotAdapter
        services.AddKeyedSingleton<CompatBotAdapter>(keyName, (sp, key) =>
        {
            return new CompatBotAdapter(
                sp,
                sp.GetRequiredService<IHttpContextAccessor>(),
                sp.GetRequiredService<ILogger<CompatBotAdapter>>(),
                key.ToString()!);
        });
    }

    /// <summary>
    /// Gets or creates a logger from the service collection without building the service provider.
    /// </summary>
    private static ILogger GetOrCreateLogger(IServiceCollection services)
    {
        var loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        var loggerFactory = loggerFactoryDescriptor?.ImplementationInstance as ILoggerFactory;

        return loggerFactory?.CreateLogger<CompatAdapter>()
            ?? (ILogger)Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <summary>
    /// Gets the configuration from the service collection, building a temporary provider if necessary.
    /// </summary>
    private static IConfiguration GetConfiguration(IServiceCollection services)
    {
        // Try to get from service descriptors first
        var configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
        if (configDescriptor?.ImplementationInstance is IConfiguration configuration)
        {
            return configuration;
        }

        // Fall back to building a temporary provider
        using var tempProvider = services.BuildServiceProvider();
        return tempProvider.GetRequiredService<IConfiguration>();
    }
}
