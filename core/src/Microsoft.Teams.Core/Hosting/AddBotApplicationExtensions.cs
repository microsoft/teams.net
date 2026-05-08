// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    internal static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, BotConfig botConfig) where TApp : BotApplication
    {
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
    /// Microsoft.Identity.Web's registrations are TryAdd-based and safe to call multiple times.
    /// </summary>
    private static IServiceCollection EnsureMsalServices(this IServiceCollection services, BotConfig botConfig)
    {
        services.AddHttpClient()
                .AddTokenAcquisition(true)
                .AddInMemoryTokenCaches()
                .AddAgentIdentities();

        ArgumentNullException.ThrowIfNull(botConfig.MsalConfigurationSection);

        if (!string.IsNullOrWhiteSpace(botConfig.ClientId))
        {
            string sectionKey = botConfig.MsalConfigurationSection.Key;
            IConfigurationSection section = botConfig.MsalConfigurationSection;

            services.Configure<MicrosoftIdentityApplicationOptions>(sectionKey, options =>
            {
                section.Bind(options);

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
            if (!section.GetSection("ClientCredentials").GetChildren().Any())
            {
                ILogger logger = GetLoggerFromServices(services);
                logger.InferringUserAssignedManagedIdentity(botConfig.ClientId);
                services.Configure<ManagedIdentityOptions>(options =>
                {
                    options.UserAssignedClientId = botConfig.ClientId;
                });
            }
        }
        return services;
    }

    internal static IServiceCollection AddBotClient<TClient>(
        this IServiceCollection services,
        string httpClientName,
        BotConfig botConfig) where TClient : class
    {
        if (!string.IsNullOrWhiteSpace(botConfig.ClientId))
        {
            string scope = botConfig.Scope;
            services.AddHttpClient<TClient>(httpClientName)
                .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    scope,
                    botConfig.SectionName,
                    sp.GetService<IOptions<ManagedIdentityOptions>>()));
        }
        else
        {
            services.AddHttpClient<TClient>(httpClientName);
        }
        return services;
    }

    /// <summary>
    /// Gets a logger instance from the service collection.
    /// If the logger factory is not available as an instance, builds a temporary service provider to create the logger.
    /// </summary>
    /// <param name="services">The service collection to extract the logger from.</param>
    /// <param name="categoryType">The type to use for the logger category. If null, uses AddBotApplicationExtensions.</param>
    /// <returns>An ILogger instance, or NullLogger if no logger factory is registered.</returns>
    internal static ILogger GetLoggerFromServices(IServiceCollection services, Type? categoryType = null)
    {
        ServiceDescriptor? loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        ILoggerFactory? loggerFactory = loggerFactoryDescriptor?.ImplementationInstance as ILoggerFactory;

        // If logger factory is available as an instance, use it directly
        if (loggerFactory != null)
        {
            return loggerFactory.CreateLogger(categoryType ?? typeof(AddBotApplicationExtensions));
        }

        // Logger factory not available as a direct instance; return NullLogger
        // to avoid building a throwaway ServiceProvider during DI configuration.
        return Extensions.Logging.Abstractions.NullLogger.Instance;
    }
}
