// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Eventing.Reader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Microsoft.Bot.Core.Hosting;

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
    /// Configures the application to handle bot messages at the specified route and returns the registered bot
    /// application instance.
    /// </summary>
    /// <remarks>This method adds authentication and authorization middleware to the request pipeline and maps
    /// a POST endpoint for bot messages. The endpoint requires authorization. Ensure that the bot application is
    /// registered in the service container before calling this method.</remarks>
    /// <typeparam name="TApp">The type of the bot application to use. Must inherit from BotApplication.</typeparam>
    /// <param name="builder">The application builder used to configure the request pipeline.</param>
    /// <param name="routePath">The route path at which to listen for incoming bot messages. Defaults to "api/messages".</param>
    /// <returns>The registered bot application instance of type TApp.</returns>
    /// <exception cref="ApplicationException">Thrown if the bot application of type TApp is not registered in the application's service container.</exception>
    public static TApp UseBotApplication<TApp>(
       this IApplicationBuilder builder,
       string routePath = "api/messages")
           where TApp : BotApplication
    {
        ArgumentNullException.ThrowIfNull(builder);
        TApp app = builder.ApplicationServices.GetService<TApp>() ?? throw new InvalidOperationException("Application not registered");
        WebApplication? webApp = builder as WebApplication;
        ArgumentNullException.ThrowIfNull(webApp);
        webApp.MapPost(routePath, async (HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            CoreActivity resp = await app.ProcessAsync(httpContext, cancellationToken).ConfigureAwait(false);
            return resp.Id;
        });

        return app;
    }

    /// <summary>
    /// Adds a bot application to the service collection.
    /// </summary>
    /// <typeparam name="TApp"></typeparam>
    /// <param name="services"></param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services, string sectionName = "AzureAd") where TApp : BotApplication
    {
        services.AddConversationClient(sectionName);
        services.AddSingleton<TApp>();
        return services;
    }
       
    /// <summary>
    /// Adds a conversation client to the service collection.
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="sectionName">Configuration Section name, defaults to AzureAD</param>
    /// <returns></returns>
    public static IServiceCollection AddConversationClient(this IServiceCollection services, string sectionName = "AzureAd")
    {
        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        ArgumentNullException.ThrowIfNull(configuration);
        
        string scope = "https://api.botframework.com/.default";
        if (configuration["${sectionName}:Scopes"] is not null)
        {
            scope = configuration[$"{sectionName}:Scopes"]!;
        }
        
        if (configuration["Scope"] is not null) //ToChannelFromBotOAuthScope
        {
            scope = configuration["Scope"]!;
        }

        services
            .AddHttpClient()
            .AddTokenAcquisition(true)
            .AddInMemoryTokenCaches()
            .AddAgentIdentities();

        services.ConfigureMSAL(configuration, sectionName);
        services.AddHttpClient<ConversationClient>(ConversationClient.ConversationHttpClientName)
                .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    scope,
                    sp.GetService<IOptions<ManagedIdentityOptions>>()));
        return services;
    }

    private static IServiceCollection ConfigureMSAL(this IServiceCollection services, IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var logger = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AddBotApplicationExtensions));

        if (configuration["MicrosoftAppId"] is not null)
        {
            _logUsingBFConfig(logger, null);
            var botConfig = BotConfig.FromBFConfig(configuration);
            services.ConfigureMSALFromBotConfig(botConfig, logger);
        }
        else if (configuration["CLIENT_ID"] is not null)
        {
            _logUsingCoreConfig(logger, null);
            var botConfig = BotConfig.FromCoreConfig(configuration);
            services.ConfigureMSALFromBotConfig(botConfig, logger);
        }
        else
        {
            _logUsingSectionConfig(logger, sectionName, null);
            services.ConfigureMSALFromConfig(configuration.GetSection(sectionName));
        }
        return services;
    }

    private static IServiceCollection ConfigureMSALFromConfig(this IServiceCollection services, IConfigurationSection msalConfigSection)
    {
        ArgumentNullException.ThrowIfNull(msalConfigSection);
        services.Configure<MicrosoftIdentityApplicationOptions>(msalConfigSection);
        return services;
    }

    private static IServiceCollection ConfigureMSALWithSecret(this IServiceCollection services, string tenantId, string clientId, string clientSecret)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientSecret);

        services.Configure<MicrosoftIdentityApplicationOptions>(options =>
        {
            // TODO: Make Instance configurable
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
        ArgumentNullException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientId);

        var ficCredential = new CredentialDescription()
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
        };
        if (!string.IsNullOrEmpty(ficClientId) && !IsSystemAssignedManagedIdentity(ficClientId))
        {
            ficCredential.ManagedIdentityClientId = ficClientId;
        }

        services.Configure<MicrosoftIdentityApplicationOptions>(options =>
        {
            // TODO: Make Instance configurable
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

        // Register ManagedIdentityOptions for BotAuthenticationHandler to use
        bool isSystemAssigned = IsSystemAssignedManagedIdentity(managedIdentityClientId);
        string? umiClientId = isSystemAssigned ? null : (managedIdentityClientId ?? clientId);

        services.Configure<ManagedIdentityOptions>(options =>
        {
            options.UserAssignedClientId = umiClientId;
        });

        services.Configure<MicrosoftIdentityApplicationOptions>(options =>
        {
            // TODO: Make Instance configurable
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

    /// <summary>
    /// Determines if the provided client ID represents a system-assigned managed identity.
    /// </summary>
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

    
}
