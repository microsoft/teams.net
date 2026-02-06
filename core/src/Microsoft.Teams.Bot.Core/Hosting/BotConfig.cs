// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// Configuration model for bot authentication credentials.
/// </summary>
/// <remarks>
/// This class consolidates bot authentication settings from various configuration sources including
/// Bot Framework SDK configuration, Core configuration, and Azure AD configuration sections.
/// It supports multiple authentication modes: client secrets, system-assigned managed identities,
/// user-assigned managed identities, and federated identity credentials (FIC).
/// </remarks>
internal sealed class BotConfig
{
    /// <summary>
    /// Identifier used to specify system-assigned managed identity authentication.
    /// When FicClientId equals this value, the system will use the system-assigned managed identity.
    /// </summary>
    public const string SystemManagedIdentityIdentifier = "system";

    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application (client) ID from Azure AD app registration.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret for client credentials authentication.
    /// Optional if using managed identity or federated identity credentials.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the client ID for federated identity credentials or user-assigned managed identity.
    /// Use <see cref="SystemManagedIdentityIdentifier"/> to specify system-assigned managed identity.
    /// </summary>
    public string? FicClientId { get; set; }

    /// <summary>
    /// Creates a BotConfig from Bot Framework SDK configuration format.
    /// </summary>
    /// <param name="configuration">Configuration containing MicrosoftAppId, MicrosoftAppPassword, and MicrosoftAppTenantId settings.</param>
    /// <returns>A new BotConfig instance with settings from Bot Framework configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
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

    /// <summary>
    /// Creates a BotConfig from Teams Bot Core environment variable format.
    /// </summary>
    /// <param name="configuration">Configuration containing TENANT_ID, CLIENT_ID, CLIENT_SECRET, and MANAGED_IDENTITY_CLIENT_ID settings.</param>
    /// <returns>A new BotConfig instance with settings from Core configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <remarks>
    /// This format is typically used with environment variables in containerized deployments.
    /// The MANAGED_IDENTITY_CLIENT_ID can be set to "system" for system-assigned managed identity.
    /// </remarks>
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

    /// <summary>
    /// Creates a BotConfig from Azure AD configuration section format.
    /// </summary>
    /// <param name="configuration">Configuration containing an Azure AD configuration section.</param>
    /// <param name="sectionName">The name of the configuration section containing Azure AD settings. Defaults to "AzureAd".</param>
    /// <returns>A new BotConfig instance with settings from the Azure AD configuration section.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <remarks>
    /// This format is compatible with Microsoft.Identity.Web configuration sections in appsettings.json.
    /// The section should contain TenantId, ClientId, and optionally ClientSecret properties.
    /// </remarks>
    public static BotConfig FromAadConfig(IConfiguration configuration, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        IConfigurationSection section = configuration.GetSection(sectionName);
        return new()
        {
            TenantId = section["TenantId"] ?? string.Empty,
            ClientId = section["ClientId"] ?? string.Empty,
            ClientSecret = section["ClientSecret"],
        };
    }
}
