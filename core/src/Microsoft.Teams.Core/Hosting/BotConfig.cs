// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Core.Hosting;

/// <summary>
/// Configuration model for bot authentication credentials, sourced from a
/// Microsoft.Identity.Web compatible configuration section (e.g. "AzureAd").
/// </summary>
internal sealed class BotConfig
{
    internal const string DefaultSectionName = "AzureAd";

    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application (client) ID from Azure AD app registration.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration section name used to resolve this BotConfig.
    /// Also used as the MSAL named-options key and the JWT auth scheme name.
    /// </summary>
    public string SectionName { get; set; } = DefaultSectionName;

    internal IConfigurationSection? MsalConfigurationSection { get; set; }

    /// <summary>
    /// Gets a value indicating whether this configuration uses User-Assigned Managed Identity (UMI) for authentication.
    /// Returns true when no ClientCredentials are configured in the section.
    /// </summary>
    internal bool IsUserAssignedManagedIdentity =>
        MsalConfigurationSection is not null &&
        !MsalConfigurationSection.GetSection("ClientCredentials").GetChildren().Any();

    /// <summary>
    /// Resolves a BotConfig from a service collection by extracting configuration and logger.
    /// </summary>
    /// <param name="services">The service collection containing IConfiguration and ILoggerFactory registrations.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "AzureAd".</param>
    /// <returns>A BotConfig populated from the section, or an empty BotConfig if no ClientId is configured.</returns>
    public static BotConfig Resolve(IServiceCollection services, string sectionName = DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Extract IConfiguration from service collection — prefer the instance if available,
        // otherwise resolve via the factory registered in the descriptor.
        ServiceDescriptor? configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
        IConfiguration? configuration = configDescriptor?.ImplementationInstance as IConfiguration;
        if (configuration is null && configDescriptor?.ImplementationFactory is not null)
        {
            using ServiceProvider tempProvider = services.BuildServiceProvider();
            configuration = tempProvider.GetService<IConfiguration>();
        }

        if (configuration is null)
        {
            throw new InvalidOperationException(
                "IConfiguration must be registered in the service collection before calling BotConfig.Resolve. " +
                "Ensure AddConfiguration() or WebApplication.CreateBuilder() has been called.");
        }

        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services, typeof(BotConfig));

        IConfigurationSection section = configuration.GetSection(sectionName);
        BotConfig config = new()
        {
            TenantId = section["TenantId"] ?? string.Empty,
            ClientId = section["ClientId"] ?? string.Empty,
            MsalConfigurationSection = section,
            SectionName = sectionName
        };

        if (!string.IsNullOrEmpty(config.ClientId))
        {
            _logUsingSectionConfig(logger, sectionName, null);
        }
        else
        {
            _logNoConfigFound(logger, null);
        }
        return config;
    }

    private static readonly Action<ILogger, string, Exception?> _logUsingSectionConfig =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Resolved bot configuration from '{SectionName}' configuration section");
    private static readonly Action<ILogger, Exception?> _logNoConfigFound =
        LoggerMessage.Define(LogLevel.Warning, new(4), "No bot configuration found in configuration.");

}
