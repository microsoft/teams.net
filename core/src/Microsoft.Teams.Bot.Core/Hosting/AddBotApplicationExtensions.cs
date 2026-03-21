// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    internal const string MsalConfigKey = "AzureAd";

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
    /// <typeparam name="TApp">The type of the bot application to use. Must inherit from BotApplication.</typeparam>
    /// <param name="endpoints">The endpoint route builder used to configure endpoints.</param>
    /// <param name="routePath">The route path at which to listen for incoming bot messages. Defaults to "api/messages".</param>
    /// <returns>The registered bot application instance of type TApp.</returns>
    public static TApp UseBotApplication<TApp>(
       this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
           where TApp : BotApplication
    {
        ArgumentNullException.ThrowIfNull(endpoints);

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
    public static IServiceCollection AddBotApplication(this IServiceCollection services, string sectionName = "AzureAd")
        => services.AddBotApplication<BotApplication>(sectionName);

    /// <summary>
    /// Adds a bot application to the service collection.
    /// </summary>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, string sectionName = "AzureAd") where TApp : BotApplication
    {
        var (configuration, logger) = ResolveConfigAndLogger(services);

        services.AddSingleton<BotApplicationOptions>(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            return new BotApplicationOptions
            {
                AppId = config["MicrosoftAppId"] ?? config["CLIENT_ID"] ?? config[$"{sectionName}:ClientId"] ?? string.Empty
            };
        });
        services.AddHttpContextAccessor();
        services.AddBotAuthorization(sectionName, logger);

        // Configure shared infrastructure (options, token acquisition, etc.) once
        RegisterSharedInfrastructure(services, sectionName);

        // Check MSAL configuration once, register both clients with the result
        bool msalConfigured = services.ConfigureMSAL(configuration, sectionName, logger);
        if (!msalConfigured)
        {
            _logAuthConfigNotFound(logger, null);
        }

        AddBotClient<ConversationClient>(services, ConversationClient.ConversationHttpClientName, msalConfigured);
        AddBotClient<UserTokenClient>(services, UserTokenClient.UserTokenHttpClientName, msalConfigured);

        services.AddSingleton<TApp>();
        return services;
    }

    /// <summary>
    /// Adds conversation client to the service collection.
    /// </summary>
    public static IServiceCollection AddConversationClient(this IServiceCollection services, string sectionName = "AzureAd")
    {
        var (configuration, logger) = ResolveConfigAndLogger(services);
        RegisterSharedInfrastructure(services, sectionName);
        bool msalConfigured = services.ConfigureMSAL(configuration, sectionName, logger);
        if (!msalConfigured)
        {
            _logAuthConfigNotFound(logger, null);
        }

        return AddBotClient<ConversationClient>(services, ConversationClient.ConversationHttpClientName, msalConfigured);
    }

    /// <summary>
    /// Adds user token client to the service collection.
    /// </summary>
    public static IServiceCollection AddUserTokenClient(this IServiceCollection services, string sectionName = "AzureAd")
    {
        var (configuration, logger) = ResolveConfigAndLogger(services);
        RegisterSharedInfrastructure(services, sectionName);
        bool msalConfigured = services.ConfigureMSAL(configuration, sectionName, logger);
        if (!msalConfigured)
        {
            _logAuthConfigNotFound(logger, null);
        }

        return AddBotClient<UserTokenClient>(services, UserTokenClient.UserTokenHttpClientName, msalConfigured);
    }

    private static (IConfiguration configuration, ILogger logger) ResolveConfigAndLogger(IServiceCollection services)
    {
        ServiceDescriptor? configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
        ServiceDescriptor? loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        ILoggerFactory? loggerFactory = loggerFactoryDescriptor?.ImplementationInstance as ILoggerFactory;

        if (configDescriptor?.ImplementationInstance is IConfiguration config)
        {
            ILogger logger = loggerFactory?.CreateLogger(typeof(AddBotApplicationExtensions))
                ?? Extensions.Logging.Abstractions.NullLogger.Instance;
            return (config, logger);
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        IConfiguration resolvedConfig = tempProvider.GetRequiredService<IConfiguration>();
        ILogger resolvedLogger = (loggerFactory ?? tempProvider.GetRequiredService<ILoggerFactory>())
            .CreateLogger(typeof(AddBotApplicationExtensions));
        return (resolvedConfig, resolvedLogger);
    }

    private static void RegisterSharedInfrastructure(IServiceCollection services, string sectionName)
    {
        services.AddOptions<BotClientOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.Scope = "https://api.botframework.com/.default";
                if (!string.IsNullOrEmpty(configuration[$"{sectionName}:Scope"]))
                    options.Scope = configuration[$"{sectionName}:Scope"]!;
                if (!string.IsNullOrEmpty(configuration["Scope"]))
                    options.Scope = configuration["Scope"]!;
                options.SectionName = sectionName;
            });

        services
            .AddHttpClient()
            .AddTokenAcquisition(true)
            .AddInMemoryTokenCaches()
            .AddAgentIdentities();
    }

    private static IServiceCollection AddBotClient<TClient>(
        IServiceCollection services,
        string httpClientName,
        bool msalConfigured) where TClient : class
    {
        if (msalConfigured)
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
            services.AddHttpClient<TClient>(httpClientName);
        }

        return services;
    }

    private static bool ConfigureMSAL(this IServiceCollection services, IConfiguration configuration, string sectionName, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration["MicrosoftAppId"] is not null)
        {
            _logUsingBFConfig(logger, null);
            BotConfig botConfig = BotConfig.FromBFConfig(configuration);
            services.ConfigureMSALFromBotConfig(botConfig, logger);
            return true;
        }

        if (configuration["CLIENT_ID"] is not null)
        {
            _logUsingCoreConfig(logger, null);
            BotConfig botConfig = BotConfig.FromCoreConfig(configuration);
            services.ConfigureMSALFromBotConfig(botConfig, logger);
            return true;
        }

        _logUsingSectionConfig(logger, sectionName, null);
        var section = configuration.GetSection(sectionName);
        if (section["ClientId"] is not null && !string.IsNullOrEmpty(section["ClientId"]))
        {
            services.ConfigureMSALFromConfig(section);
            return true;
        }

        return false;
    }

    private static IServiceCollection ConfigureMSALFromConfig(this IServiceCollection services, IConfigurationSection msalConfigSection)
    {
        ArgumentNullException.ThrowIfNull(msalConfigSection);
        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, msalConfigSection);
        return services;
    }

    private static IServiceCollection ConfigureMSALWithSecret(this IServiceCollection services, string tenantId, string clientId, string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = tenantId;
            options.ClientId = clientId;
            options.ClientCredentials = [
                new CredentialDescription()
                {
                   SourceType = CredentialSource.ClientSecret,
                   ClientSecret = clientSecret
                }
            ];
        });
        return services;
    }

    private static IServiceCollection ConfigureMSALWithFIC(this IServiceCollection services, string tenantId, string clientId, string? ficClientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        CredentialDescription ficCredential = new()
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
        };
        if (!string.IsNullOrEmpty(ficClientId) && !IsSystemAssignedManagedIdentity(ficClientId))
        {
            ficCredential.ManagedIdentityClientId = ficClientId;
        }

        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = tenantId;
            options.ClientId = clientId;
            options.ClientCredentials = [
                ficCredential
            ];
        });
        return services;
    }

    private static IServiceCollection ConfigureMSALWithUMI(this IServiceCollection services, string tenantId, string clientId, string? managedIdentityClientId = null)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientId);

        bool isSystemAssigned = IsSystemAssignedManagedIdentity(managedIdentityClientId);
        string? umiClientId = isSystemAssigned ? null : (managedIdentityClientId ?? clientId);

        services.Configure<ManagedIdentityOptions>(options =>
        {
            options.UserAssignedClientId = umiClientId;
        });

        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = tenantId;
            options.ClientId = clientId;
        });
        return services;
    }

    private static IServiceCollection ConfigureMSALFromBotConfig(this IServiceCollection services, BotConfig botConfig, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(botConfig);
        if (!string.IsNullOrEmpty(botConfig.ClientSecret))
        {
            _logUsingClientSecret(logger, null);
            services.ConfigureMSALWithSecret(botConfig.TenantId, botConfig.ClientId, botConfig.ClientSecret);
        }
        else if (string.IsNullOrEmpty(botConfig.FicClientId) || botConfig.FicClientId == botConfig.ClientId)
        {
            _logUsingUMI(logger, null);
            services.ConfigureMSALWithUMI(botConfig.TenantId, botConfig.ClientId, botConfig.FicClientId);
        }
        else
        {
            bool isSystemAssigned = IsSystemAssignedManagedIdentity(botConfig.FicClientId);
            _logUsingFIC(logger, isSystemAssigned ? "System-Assigned" : "User-Assigned", null);
            services.ConfigureMSALWithFIC(botConfig.TenantId, botConfig.ClientId, botConfig.FicClientId);
        }
        return services;
    }

    private static bool IsSystemAssignedManagedIdentity(string? clientId)
        => string.Equals(clientId, BotConfig.SystemManagedIdentityIdentifier, StringComparison.OrdinalIgnoreCase);

    private static readonly Action<ILogger, Exception?> _logUsingBFConfig =
        LoggerMessage.Define(LogLevel.Debug, new(1), "Configuring MSAL from Bot Framework configuration");
    private static readonly Action<ILogger, Exception?> _logUsingCoreConfig =
        LoggerMessage.Define(LogLevel.Debug, new(2), "Configuring MSAL from Core bot configuration");
    private static readonly Action<ILogger, string, Exception?> _logUsingSectionConfig =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Configuring MSAL from {SectionName} configuration section");
    private static readonly Action<ILogger, Exception?> _logUsingClientSecret =
        LoggerMessage.Define(LogLevel.Debug, new(4), "Configuring authentication with client secret");
    private static readonly Action<ILogger, Exception?> _logUsingUMI =
        LoggerMessage.Define(LogLevel.Debug, new(5), "Configuring authentication with User-Assigned Managed Identity");
    private static readonly Action<ILogger, string, Exception?> _logUsingFIC =
        LoggerMessage.Define<string>(LogLevel.Debug, new(6), "Configuring authentication with Federated Identity Credential (Managed Identity) with {IdentityType} Managed Identity");
    private static readonly Action<ILogger, Exception?> _logAuthConfigNotFound =
        LoggerMessage.Define(LogLevel.Warning, new(7), "Authentication configuration not found. Running without Auth");
}
