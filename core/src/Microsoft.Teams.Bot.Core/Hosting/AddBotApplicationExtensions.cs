// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Teams.Bot.Core.Hosting;

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
    /// Initializes the default route
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="routePath"></param>
    /// <returns></returns>
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
    /// Adds a bot application to the service collection with the default configuration section name "AzureAd".
    /// </summary>
    /// <param name="services"></param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotApplication(this IServiceCollection services, string sectionName = "AzureAd")
        => services.AddBotApplication<BotApplication>(sectionName);

    /// <summary>
    /// Adds a bot application to the service collection.
    /// </summary>
    /// <typeparam name="TApp"></typeparam>
    /// <param name="services"></param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, string sectionName = "AzureAd") where TApp : BotApplication
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);

        services.AddSingleton<BotApplicationOptions>(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            return new BotApplicationOptions
            {
                AppId = botConfig.ClientId
            };
        });
        services.AddHttpContextAccessor();
        services.AddBotAuthorization(aadSectionName: botConfig.SectionName);
        services.AddConversationClient(botConfig);
        services.AddUserTokenClient(botConfig);
        services.AddSingleton<TApp>();
        return services;
    }

    /// <summary>
    /// Adds conversation client to the service collection.
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="sectionName">Configuration Section name, defaults to AzureAD</param>
    /// <returns></returns>
    public static IServiceCollection AddConversationClient(this IServiceCollection services, string sectionName = "AzureAd")
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);
        return services.AddConversationClient(botConfig);
    }

    /// <summary>
    /// Adds user token client to the service collection.
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="sectionName">Configuration Section name, defaults to AzureAD</param>
    /// <returns></returns>
    public static IServiceCollection AddUserTokenClient(this IServiceCollection services, string sectionName = "AzureAd")
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);
        return services.AddUserTokenClient(botConfig);
    }

    /// <summary>
    /// Adds conversation client to the service collection using an already-resolved BotConfig.
    /// </summary>
    private static IServiceCollection AddConversationClient(this IServiceCollection services, BotConfig botConfig) =>
        services.AddBotClient<ConversationClient>(ConversationClient.ConversationHttpClientName, botConfig);

    /// <summary>
    /// Adds user token client to the service collection using an already-resolved BotConfig.
    /// </summary>
    private static IServiceCollection AddUserTokenClient(this IServiceCollection services, BotConfig botConfig) =>
        services.AddBotClient<UserTokenClient>(UserTokenClient.UserTokenHttpClientName, botConfig);

    internal static IServiceCollection AddBotClient<TClient>(
        this IServiceCollection services,
        string httpClientName,
        BotConfig botConfig) where TClient : class
    {
        // Register options using values from BotConfig
        services.AddOptions<BotClientOptions>()
            .Configure(options =>
            {
                options.Scope = botConfig.Scope;
                options.SectionName = botConfig.SectionName;
            });

        services
            .AddHttpClient()
            .AddTokenAcquisition(true)
            .AddInMemoryTokenCaches()
            .AddAgentIdentities();

        ILogger logger = GetLoggerFromServices(services);

        if (services.ConfigureMSAL(botConfig, logger))
        {
            services.AddHttpClient<TClient>(httpClientName)
                .AddHttpMessageHandler(sp =>
                {
                    BotClientOptions botOptions = sp.GetRequiredService<IOptions<BotClientOptions>>().Value;
                    return new BotAuthenticationHandler(
                        sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                        sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                        botOptions.Scope,
                        sp.GetService<IOptions<ManagedIdentityOptions>>());
                });
        }
        else
        {
            _logAuthConfigNotFound(logger, httpClientName, null);
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

        // Otherwise, build a temporary service provider to create the logger
        using ServiceProvider tempProvider = services.BuildServiceProvider();
        ILoggerFactory? tempFactory = tempProvider.GetService<ILoggerFactory>();
        return (ILogger?)tempFactory?.CreateLogger(categoryType ?? typeof(AddBotApplicationExtensions))
            ?? Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    private static readonly Action<ILogger, string, Exception?> _logAuthConfigNotFound =
        LoggerMessage.Define<string>(LogLevel.Warning, new(7), "Authentication configuration not found. Outgoing requests from '{HttpClientName}' will not be authenticated.");
}
