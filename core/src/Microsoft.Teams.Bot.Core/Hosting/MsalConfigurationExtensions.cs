// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;

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
            services.ConfigureMSALFromConfig(botConfig.MsalConfigurationSection, botConfig.Cloud);
        }
        else
        {
            services.ConfigureMSALFromBotConfig(botConfig, logger);
        }

        return true;
    }

    private static IServiceCollection ConfigureMSALFromConfig(this IServiceCollection services, IConfigurationSection msalConfigSection, CloudEnvironment cloud)
    {
        ArgumentNullException.ThrowIfNull(msalConfigSection);

        string? sectionInstance = msalConfigSection["Instance"];
        string? sectionAuthority = msalConfigSection["Authority"];
        ValidateSectionMatchesCloud(msalConfigSection.Path, sectionInstance, sectionAuthority, cloud);

        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, msalConfigSection);

        // Fall back to Cloud's login endpoint when the section didn't set Instance or Authority.
        // Lets `Cloud: "USGov"` alone configure sovereign correctly; honors the section when it's explicit.
        if (string.IsNullOrWhiteSpace(sectionInstance) && string.IsNullOrWhiteSpace(sectionAuthority))
        {
            services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, options =>
            {
                options.Instance = cloud.LoginEndpoint + "/";
            });
        }
        return services;
    }

    private static void ValidateSectionMatchesCloud(string sectionPath, string? sectionInstance, string? sectionAuthority, CloudEnvironment cloud)
    {
        string expected = NormalizeEndpoint(cloud.LoginEndpoint);

        if (!string.IsNullOrWhiteSpace(sectionInstance) &&
            !NormalizeEndpoint(sectionInstance).Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"MSAL configuration conflict: '{sectionPath}:Instance' is '{sectionInstance}' " +
                $"but Cloud resolves login endpoint to '{cloud.LoginEndpoint}'. " +
                $"Either remove Instance from the MSAL section (Cloud will set it) or change Cloud to match.");
        }

        if (!string.IsNullOrWhiteSpace(sectionAuthority) &&
            !NormalizeEndpoint(sectionAuthority).StartsWith(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"MSAL configuration conflict: '{sectionPath}:Authority' is '{sectionAuthority}' " +
                $"but Cloud resolves login endpoint to '{cloud.LoginEndpoint}'. " +
                $"Either remove Authority from the MSAL section (Cloud will set Instance) or change Cloud to match.");
        }
    }

    private static string NormalizeEndpoint(string value) => value.TrimEnd('/');

    private static IServiceCollection ConfigureMSALWithSecret(this IServiceCollection services, string tenantId, string clientId, string clientSecret, CloudEnvironment cloud)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        services.Configure<MicrosoftIdentityApplicationOptions>(MsalConfigKey, options =>
        {
            options.Instance = cloud.LoginEndpoint + "/";
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

    private static IServiceCollection ConfigureMSALWithFIC(this IServiceCollection services, string tenantId, string clientId, string? ficClientId, CloudEnvironment cloud)
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
            options.Instance = cloud.LoginEndpoint + "/";
            options.TenantId = tenantId;
            options.ClientId = clientId;
            options.ClientCredentials = [
                ficCredential
            ];
        });
        return services;
    }

    private static IServiceCollection ConfigureMSALWithUMI(this IServiceCollection services, string tenantId, string clientId, CloudEnvironment cloud, string? managedIdentityClientId = null)
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
            options.Instance = cloud.LoginEndpoint + "/";
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
            services.ConfigureMSALWithSecret(botConfig.TenantId, botConfig.ClientId, botConfig.ClientSecret, botConfig.Cloud);
        }
        else if (string.IsNullOrEmpty(botConfig.FicClientId) || botConfig.FicClientId == botConfig.ClientId)
        {
            _logUsingUMI(logger, null);
            services.ConfigureMSALWithUMI(botConfig.TenantId, botConfig.ClientId, botConfig.Cloud, botConfig.FicClientId);
        }
        else
        {
            bool isSystemAssigned = IsSystemAssignedManagedIdentity(botConfig.FicClientId);
            _logUsingFIC(logger, isSystemAssigned ? "System-Assigned" : "User-Assigned", null);
            services.ConfigureMSALWithFIC(botConfig.TenantId, botConfig.ClientId, botConfig.FicClientId, botConfig.Cloud);
        }
        return services;
    }

    private static bool IsSystemAssignedManagedIdentity(string? clientId)
        => string.Equals(clientId, BotConfig.SystemManagedIdentityIdentifier, StringComparison.OrdinalIgnoreCase);

    private static readonly Action<ILogger, Exception?> _logUsingClientSecret =
        LoggerMessage.Define(LogLevel.Debug, new(1), "Configuring authentication with client secret");
    private static readonly Action<ILogger, Exception?> _logUsingUMI =
        LoggerMessage.Define(LogLevel.Debug, new(2), "Configuring authentication with User-Assigned Managed Identity");
    private static readonly Action<ILogger, string, Exception?> _logUsingFIC =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Configuring authentication with Federated Identity Credential (Managed Identity) with {IdentityType} Managed Identity");
}
