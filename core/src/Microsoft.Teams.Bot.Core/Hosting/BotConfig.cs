// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Teams.Bot.Core.Hosting;

/// <summary>
/// Configuration model for bot authentication credentials.
/// </summary>
/// <remarks>
/// This class consolidates bot authentication settings from various configuration sources including
/// Bot Framework SDK configuration, Core configuration, and MSAL configuration sections.
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

    private const string BotScope = "https://api.botframework.com/.default";

    private const string DefaultSectionName = "AzureAd";

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
    /// Gets or sets the configuration section name used to resolve this BotConfig.
    /// </summary>
    public string SectionName { get; set; } = DefaultSectionName;

    /// <summary>
    /// Gets or sets the scope for token acquisition.
    /// Defaults to "https://api.botframework.com/.default" if not specified.
    /// </summary>
    public string Scope { get; set; } = BotScope;

    internal IConfigurationSection? MsalConfigurationSection { get; set; }

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
            Scope = configuration["Scope"] ?? BotScope
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
            Scope = configuration["Scope"] ?? BotScope,
        };
    }

    /// <summary>
    /// Creates a BotConfig from MSAL configuration section format.
    /// </summary>
    /// <param name="configuration">Configuration containing an MSAL configuration section.</param>
    /// <param name="sectionName">The name of the configuration section containing MSAL settings. Defaults to "AzureAd".</param>
    /// <returns>A new BotConfig instance with settings from the MSAL configuration section.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <remarks>
    /// This format is compatible with Microsoft.Identity.Web configuration sections in appsettings.json.
    /// The section should contain TenantId, ClientId, and optionally ClientSecret properties.
    /// </remarks>
    public static BotConfig FromMsalConfig(IConfiguration configuration, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        IConfigurationSection section = configuration.GetSection(sectionName);
        return new()
        {
            TenantId = section["TenantId"] ?? string.Empty,
            ClientId = section["ClientId"] ?? string.Empty,
            ClientSecret = section["ClientSecret"],
            Scope = section["Scope"] ?? BotScope,
            MsalConfigurationSection = section,
            SectionName = sectionName
        };
    }

    /// <summary>
    /// Resolves a BotConfig from a service collection by extracting configuration and logger,
    /// then trying all configuration formats in priority order.
    /// </summary>
    /// <param name="services">The service collection containing IConfiguration and ILoggerFactory registrations.</param>
    /// <param name="sectionName">The MSAL configuration section name. Defaults to "AzureAd".</param>
    /// <returns>The first BotConfig with a non-empty ClientId, or a BotConfig with empty ClientId if none is found.</returns>
    public static BotConfig Resolve(IServiceCollection services, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(services);

        // Extract IConfiguration from service collection
        ServiceDescriptor? configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
        IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
            ?? services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        // Get logger using the helper method from AddBotApplicationExtensions
        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services, typeof(BotConfig));

        return Resolve(configuration, sectionName, logger);
    }

    /// <summary>
    /// Resolves a BotConfig by trying all configuration formats in priority order:
    /// MSAL section, Core environment variables, then Bot Framework SDK keys.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="sectionName">The MSAL configuration section name. Defaults to "AzureAd".</param>
    /// <param name="logger">Optional logger to log which configuration source was used.</param>
    /// <returns>The first BotConfig with a non-empty ClientId, or a BotConfig with empty ClientId if none is found.</returns>
    public static BotConfig Resolve(IConfiguration configuration, string sectionName = "AzureAd", ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        logger ??= NullLogger.Instance;

        BotConfig config = FromMsalConfig(configuration, sectionName);
        if (!string.IsNullOrEmpty(config.ClientId))
        {
            _logUsingSectionConfig(logger, sectionName, null);
            config.SectionName = sectionName;
            return config;
        }

        config = FromCoreConfig(configuration);
        if (!string.IsNullOrEmpty(config.ClientId))
        {
            _logUsingCoreConfig(logger, null);
            config.SectionName = sectionName;
            return config;
        }

        config = FromBFConfig(configuration);
        if (!string.IsNullOrEmpty(config.ClientId))
        {
            _logUsingBFConfig(logger, null);
            config.SectionName = sectionName;
            return config;
        }

        // No configuration found - log warning and return empty config
        _logNoConfigFound(logger, null);
        return new BotConfig { SectionName = sectionName };
    }

    private static readonly Action<ILogger, Exception?> _logUsingBFConfig =
        LoggerMessage.Define(LogLevel.Debug, new(1), "Resolved bot configuration from Bot Framework configuration keys");
    private static readonly Action<ILogger, Exception?> _logUsingCoreConfig =
        LoggerMessage.Define(LogLevel.Debug, new(2), "Resolved bot configuration from Core environment variables");
    private static readonly Action<ILogger, string, Exception?> _logUsingSectionConfig =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Resolved bot configuration from '{SectionName}' configuration section");
    private static readonly Action<ILogger, Exception?> _logNoConfigFound =
        LoggerMessage.Define(LogLevel.Warning, new(4), "No bot configuration found in configuration.");

}
