// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// Provides extension methods for configuring MSAL (Microsoft Authentication Library) with different credential types.
/// </summary>
internal static class MsalConfigurationExtensions
{
    internal const string MsalConfigKey = "AzureAd";

    /// <summary>
    /// Configures MSAL authentication based on the provided BotConfig.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="botConfig">The bot configuration containing authentication settings.</param>
    /// <param name="logger">Logger for configuration messages.</param>
    /// <returns>True if MSAL was configured, false if ClientId is not present.</returns>
    internal static bool ConfigureMSAL(this IServiceCollection services, BotConfig botConfig, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(botConfig);

        if (string.IsNullOrWhiteSpace(botConfig.ClientId))
        {
            // Don't configure MSAL if ClientId is not present
            return false;
        }
        else if (botConfig.MsalConfigurationSection != null)
        {
            services.ConfigureMSALFromConfig(botConfig.MsalConfigurationSection);
        }
        else
        {
            services.ConfigureMSALFromBotConfig(botConfig, logger);
        }

        return true;
    }

    /// <summary>
    /// Configures MSAL from an IConfigurationSection.
    /// </summary>
    private static IServiceCollection ConfigureMSALFromConfig(this IServiceCollection services, IConfigurationSection msalConfigSection)
    {
        ArgumentNullException.ThrowIfNull(msalConfigSection);
        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, msalConfigSection);
        return services;
    }

    /// <summary>
    /// Configures MSAL with client secret authentication.
    /// </summary>
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

    /// <summary>
    /// Configures MSAL with Federated Identity Credential (FIC) authentication using managed identity.
    /// </summary>
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

    /// <summary>
    /// Configures MSAL with User-Assigned Managed Identity (UMI) authentication.
    /// </summary>
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

        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, options =>
        {
            options.Instance = "https://login.microsoftonline.com/";
            options.TenantId = tenantId;
            options.ClientId = clientId;
        });
        return services;
    }

    /// <summary>
    /// Configures MSAL by selecting the appropriate credential type based on BotConfig properties.
    /// </summary>
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

    private static readonly Action<ILogger, Exception?> _logUsingClientSecret =
        LoggerMessage.Define(LogLevel.Debug, new(1), "Configuring authentication with client secret");
    private static readonly Action<ILogger, Exception?> _logUsingUMI =
        LoggerMessage.Define(LogLevel.Debug, new(2), "Configuring authentication with User-Assigned Managed Identity");
    private static readonly Action<ILogger, string, Exception?> _logUsingFIC =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Configuring authentication with Federated Identity Credential (Managed Identity) with {IdentityType} Managed Identity");
}
