// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Teams.Core.Hosting;

/// <summary>
/// Provides extension methods for registering bot application clients and related authentication services with the
/// dependency injection container.
/// </summary>
/// <remarks>This class is intended to be used during application startup to configure HTTP clients, token
/// acquisition, and agent identity services required for bot-to-bot communication. The configuration section specified
/// by the Azure Active Directory (AAD) configuration name is used to bind authentication options. Typically, these
/// methods are called in the application's service configuration pipeline.</remarks>
public static class AddBotApplicationExtensions
{
    /// <summary>
    /// Configures the default <see cref="BotApplication"/> to handle bot messages at the specified route.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder used to configure endpoints.</param>
    /// <param name="routePath">The route path at which to listen for incoming bot messages. Defaults to "api/messages".</param>
    /// <returns>The registered <see cref="BotApplication"/> instance.</returns>
    public static BotApplication UseBotApplication(
        this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
        => UseBotApplication<BotApplication>(endpoints, routePath);

    /// <summary>
    /// Configures the application to handle bot messages at the specified route and returns the registered bot
    /// application instance.
    /// </summary>
    /// <remarks>This method adds authentication and authorization middleware to the HTTP pipeline and maps
    /// a POST endpoint for bot messages. The endpoint requires authorization. Ensure that the bot application
    /// is registered in the service container before calling this method.</remarks>
    /// <typeparam name="TApp">The type of the bot application to use. Must inherit from BotApplication.</typeparam>
    /// <param name="endpoints">The endpoint route builder used to configure endpoints.</param>
    /// <param name="routePath">The route path at which to listen for incoming bot messages. Defaults to "api/messages".</param>
    /// <returns>The registered bot application instance of type TApp.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the bot application of type TApp is not registered in the application's service container.</exception>
    public static TApp UseBotApplication<TApp>(
       this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
           where TApp : BotApplication
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Add authentication and authorization middleware to the pipeline
        // This is safe because WebApplication implements both IEndpointRouteBuilder and IApplicationBuilder
        if (endpoints is IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        TApp botApp = endpoints.ServiceProvider.GetService<TApp>() ?? throw new InvalidOperationException("Application not registered");

        endpoints.MapPost(routePath, (HttpContext httpContext, CancellationToken cancellationToken)
            => botApp.ProcessAsync(httpContext, cancellationToken)
        ).RequireAuthorization();

        return botApp;
    }

    /// <summary>
    /// Registers the default bot application and its dependencies in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="sectionName">The configuration section name containing Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBotApplication(this IServiceCollection services, string sectionName = BotConfig.DefaultSectionName)
        => services.AddBotApplication<BotApplication>(sectionName);

    /// <summary>
    /// Registers a custom bot application and its dependencies in the service collection.
    /// </summary>
    /// <typeparam name="TApp">The custom bot application type that inherits from BotApplication.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="sectionName">The configuration section name containing Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, string sectionName = BotConfig.DefaultSectionName) where TApp : BotApplication
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);

        services.AddBotApplication<TApp>(botConfig);

        return services;
    }

    /// <summary>
    /// Registers a custom bot application and its dependencies in the service collection.
    /// </summary>
    /// <typeparam name="TApp">The custom bot application type that inherits from BotApplication.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="botConfig">The configuration containing Azure AD settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, BotConfig botConfig) where TApp : BotApplication
    {
        ArgumentNullException.ThrowIfNull(botConfig);
        services.AddSingleton<BotApplicationOptions>(_ => new BotApplicationOptions { AppId = botConfig.ClientId });
        services.AddHttpContextAccessor();
        services.AddBotAuthorization(botConfig);
        services.EnsureMsalServices(botConfig);
        services.AddBotClient<ConversationClient>(ConversationClient.ConversationHttpClientName, botConfig);
        services.AddBotClient<UserTokenClient>(UserTokenClient.UserTokenHttpClientName, botConfig);
        services.AddSingleton<TApp>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="ConversationClient"/> and its dependencies in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="sectionName">The configuration section name containing Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddConversationClient(this IServiceCollection services, string sectionName = BotConfig.DefaultSectionName)
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);
        return services.EnsureMsalServices(botConfig)
            .AddBotClient<ConversationClient>(ConversationClient.ConversationHttpClientName, botConfig);
    }

    /// <summary>
    /// Registers the <see cref="UserTokenClient"/> and its dependencies in the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="sectionName">The configuration section name containing Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddUserTokenClient(this IServiceCollection services, string sectionName = BotConfig.DefaultSectionName)
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);
        return services.EnsureMsalServices(botConfig)
            .AddBotClient<UserTokenClient>(UserTokenClient.UserTokenHttpClientName, botConfig);
    }

    /// <summary>
    /// Registers the shared MSAL token-acquisition pipeline and binds the named MSAL options.
    /// </summary>
    /// <remarks>
    /// Safe to call multiple times: the Microsoft.Identity.Web service registrations are TryAdd-based,
    /// and the named options binding (<see cref="MicrosoftIdentityApplicationOptions"/> and
    /// <see cref="ManagedIdentityOptions"/>) appends an additional configure delegate per call. Those
    /// delegates are idempotent against the same <see cref="BotConfig"/>, so re-running them produces
    /// the same options state.
    /// </remarks>
    public static IServiceCollection EnsureMsalServices(this IServiceCollection services, BotConfig botConfig)
    {
        services.AddHttpClient()
                .AddTokenAcquisition(true)
                .AddInMemoryTokenCaches()
                .AddAgentIdentities();

        ArgumentNullException.ThrowIfNull(botConfig);
        ArgumentNullException.ThrowIfNull(botConfig.MsalConfigurationSection);

        if (!string.IsNullOrWhiteSpace(botConfig.ClientId))
        {
            services.Configure<MicrosoftIdentityApplicationOptions>(botConfig.SectionName, options =>
            {
                botConfig.MsalConfigurationSection.Bind(options);

                // Default Instance when only TenantId is configured.
                if (string.IsNullOrEmpty(options.Instance) && !string.IsNullOrEmpty(options.TenantId))
                {
                    options.Instance = "https://login.microsoftonline.com/";
                }

                // MicrosoftEntraApplicationOptions.Authority is a computed property that
                // returns Instance/TenantId/v2.0 when _authority is null.  MergedOptions
                // then sees Authority alongside Instance+TenantId and emits a warning
                // (event 500).  Setting Authority to empty prevents the computed value
                // from propagating while Instance+TenantId remain available for MSAL.
                if (!string.IsNullOrEmpty(options.Instance) && !string.IsNullOrEmpty(options.TenantId))
                {
                    options.Authority = string.Empty;
                }
            });

            // No ClientCredentials in the configured section implies pure User-Assigned Managed Identity:
            // the bot's ClientId is the UMI's clientId (as in ABS bots with the UserAssignedMSI app type).
            // Register ManagedIdentityOptions so BotAuthenticationHandler routes token acquisition through
            // the IMDS endpoint instead of the standard app-credentials flow.
            if (botConfig.IsUserAssignedManagedIdentity)
            {
                LogFromServices(services, l => l.InferringUserAssignedManagedIdentity(botConfig.ClientId));
                services.Configure<ManagedIdentityOptions>(botConfig.SectionName, options =>
                {
                    options.UserAssignedClientId = botConfig.ClientId;
                });
            }
        }
        return services;
    }

    /// <summary>
    /// Registers a typed <see cref="HttpClient"/> for <typeparamref name="TClient"/> wired to bot authentication
    /// using an already-resolved <see cref="BotConfig"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="EnsureMsalServices(IServiceCollection, BotConfig)"/> must be called on the same service
    /// collection before the resulting client is used, so that <c>IAuthorizationHeaderProvider</c> and the
    /// named MSAL options are registered.
    /// </remarks>
    /// <typeparam name="TClient">The client class to register the named <see cref="HttpClient"/> for.</typeparam>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="httpClientName">The named <see cref="HttpClient"/> registration to associate with <typeparamref name="TClient"/>.</param>
    /// <param name="botConfig">The resolved bot configuration containing tenant and client settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBotClient<TClient>(
        this IServiceCollection services,
        string httpClientName,
        BotConfig botConfig) where TClient : class
    {
        ArgumentNullException.ThrowIfNull(botConfig);
        if (!string.IsNullOrWhiteSpace(botConfig.ClientId))
        {
            services.AddHttpClient<TClient>(httpClientName)
                .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    botConfig.SectionName,
                    sp.GetService<IOptionsMonitor<ManagedIdentityOptions>>()));
        }
        else
        {
            services.AddHttpClient<TClient>(httpClientName);
        }
        return services;
    }

    /// <summary>
    /// Registers a named <see cref="HttpClient"/> wired to bot authentication
    /// using an already-resolved <see cref="BotConfig"/>, without binding it to a typed client.
    /// Use this when the client type will be registered separately via a factory.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="httpClientName">The logical name for this <see cref="HttpClient"/> registration.</param>
    /// <param name="botConfig">The resolved bot configuration containing tenant and client settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBotHttpClient(
        this IServiceCollection services,
        string httpClientName,
        BotConfig botConfig)
    {
        ArgumentNullException.ThrowIfNull(botConfig);
        if (!string.IsNullOrWhiteSpace(botConfig.ClientId))
        {
            services.AddHttpClient(httpClientName)
                .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    botConfig.SectionName,
                    sp.GetService<IOptionsMonitor<ManagedIdentityOptions>>()));
        }
        else
        {
            services.AddHttpClient(httpClientName);
        }
        return services;
    }

    /// <summary>
    /// Resolves a service from the service collection before the host is built,
    /// preferring a direct instance and falling back to building a temporary
    /// <see cref="ServiceProvider"/> when the service is registered via factory or type.
    /// </summary>
    /// <remarks>
    /// The temporary <see cref="ServiceProvider"/> is disposed before the method returns.
    /// Only use this for services whose resolved instances remain valid after their
    /// owning provider is disposed (e.g. <see cref="IConfiguration"/>). Do NOT use for
    /// disposable services like <see cref="ILoggerFactory"/> — see
    /// <see cref="LogFromServices"/> for that case.
    /// </remarks>
    internal static T? ResolveFromServicesPreHost<T>(IServiceCollection services) where T : class
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is null)
        {
            return null;
        }

        if (descriptor.ImplementationInstance is T instance)
        {
            return instance;
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        return tempProvider.GetService<T>();
    }

    internal static void LogFromServices(IServiceCollection services, Action<ILogger> action, Type? categoryType = null)
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (descriptor is null)
        {
            action(NullLogger.Instance);
            return;
        }

        if (descriptor.ImplementationInstance is ILoggerFactory directFactory)
        {
            action(directFactory.CreateLogger(categoryType ?? typeof(AddBotApplicationExtensions)));
            return;
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        ILoggerFactory? factory = tempProvider.GetService<ILoggerFactory>();
        action(factory?.CreateLogger(categoryType ?? typeof(AddBotApplicationExtensions)) ?? NullLogger.Instance);
    }
}
