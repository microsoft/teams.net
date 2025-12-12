// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
[SuppressMessage("Compiler","CS1591:",Justification = "WIP")]
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
    /// <returns></returns>
    public static IServiceCollection AddBotApplication<TApp>(this IServiceCollection services) where TApp : BotApplication
    {
        services.AddBotApplicationClients();
        services.AddSingleton<TApp>();
        return services;
    }

    /// <summary>
    /// Adds and configures Bot Framework application clients and related authentication services to the specified
    /// service collection.
    /// </summary>
    /// <remarks>This method registers HTTP clients, token acquisition, in-memory token caching, and agent
    /// identity services required for Bot Framework integration. It also configures authentication options using the
    /// specified Azure AD configuration section. The method should be called during application startup as part of
    /// service configuration.</remarks>
    /// <param name="services">The service collection to which the Bot Framework clients and authentication services will be added. Must not be
    /// null.</param>
    /// <param name="msalConfigSectionKey">The name of the configuration section containing Azure Active Directory settings. Defaults to "AzureAd" if not
    /// specified.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddBotApplicationClients(this IServiceCollection services, string msalConfigSectionKey = "AzureAd")
    {
        services.AddMSAL();

        IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var msalConfigSection = configuration.GetSection(msalConfigSectionKey);

        services.Configure<MicrosoftIdentityApplicationOptions>(msalConfigSection.Key, msalConfigSection);

        string agentScope = configuration[$"{msalConfigSectionKey}:Scope"] ?? "https://api.botframework.com/.default";

        if (configuration.GetSection(msalConfigSectionKey).Get<MicrosoftIdentityApplicationOptions>() is null)
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            services.BuildServiceProvider().GetRequiredService<ILoggerFactory>()
                .CreateLogger("AddBotApplicationExtensions")
                .LogWarning("No configuration found for section {AadConfigSectionName}. BotAuthenticationHandler will not be configured.", msalConfigSectionKey);
#pragma warning restore CA1848 // Use the LoggerMessage delegates

            services.AddHttpClient<ConversationClient>(ConversationClient.ConversationHttpClientName);
        }
        else
        {
            services.AddConversationClient(agentScope);
        }

        return services;
    }

    public static IServiceCollection AddMSAL(this IServiceCollection services)
    {
        services
            .AddHttpClient()
            .AddTokenAcquisition(true)
            .AddInMemoryTokenCaches()
            .AddAgentIdentities();
        return services;
    }

    public static IServiceCollection AddConversationClient(this IServiceCollection services, string agentScope = "https://api.botframework.com/.default")
    {
        services.AddMSAL();
        services.AddHttpClient<ConversationClient>(ConversationClient.ConversationHttpClientName)
                .AddHttpMessageHandler(sp => new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    agentScope,
                    ConversationClient.ConversationHttpClientName));
        return services;
    }

    public static IServiceCollection ConfigureMSALFromConfig(this IServiceCollection services, IConfigurationSection msalConfigSection)
    {
        ArgumentNullException.ThrowIfNull(msalConfigSection);
        services.Configure<MicrosoftIdentityApplicationOptions>(msalConfigSection);
        return services;
    }

    public static IServiceCollection ConfigureMSALWithSecret(this IServiceCollection services, string tenantId, string clientId, string clientSecret)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientSecret);

        services.Configure<MicrosoftIdentityApplicationOptions>(options =>
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

    public static IServiceCollection ConfigureMSALWithFIC(this IServiceCollection services, string tenantId, string clientId, string? ficClientId)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(clientId);

        var ficCredential = new CredentialDescription()
        {
            SourceType = CredentialSource.SignedAssertionFromManagedIdentity,
        };
        if (!string.IsNullOrEmpty(ficClientId) || clientId != ficClientId)
        {
            ficCredential.ManagedIdentityClientId = ficClientId;
        }

        services.Configure<MicrosoftIdentityApplicationOptions>(options =>
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

    private static IServiceCollection ConfigureMSALFromBotConfig(this IServiceCollection services, BotConfig botConfig)
    {
        ArgumentNullException.ThrowIfNull(botConfig);
        if (!string.IsNullOrEmpty(botConfig.ClientSecret))
        {
            services.ConfigureMSALWithSecret(botConfig.TenantId, botConfig.ClientId, botConfig.ClientSecret);
        }
        else
        {
            services.ConfigureMSALWithFIC(botConfig.TenantId, botConfig.ClientId, botConfig.FicClientId);
        }
        return services;
    }

    public static IServiceCollection ConfigureMSALFromBFConfig(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var botConfig = BotConfig.FromBFConfig(configuration);
        return services.ConfigureMSALFromBotConfig(botConfig);
    }

    public static IServiceCollection ConfigureMSALFromCoreConfig(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var botConfig = BotConfig.FromCoreConfig(configuration);
        return services.ConfigureMSALFromBotConfig(botConfig);
    }

    public static IServiceCollection UseConfig(this IServiceCollection services)
    {

        return services;
    }
}

public class BotConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string? FicClientId { get; set; }

    public static BotConfig FromBFConfig(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new()
        {
            TenantId = configuration["MicrosoftAppTenantId"] ?? string.Empty,
            ClientId = configuration["MicrosoftAppId"] ?? string.Empty,
            ClientSecret = configuration["MicrosoftAppPassword"],
        };
    }

    public static BotConfig FromCoreConfig(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new()
        {
            TenantId = configuration["TENANT_ID"] ?? string.Empty,
            ClientId = configuration["CLIENT_ID"] ?? string.Empty,
            ClientSecret = configuration["CLIENT_SECRET"],
            FicClientId = configuration["MANAGED_IDENTITY_CLIENT_ID"],
        };
    }
}
