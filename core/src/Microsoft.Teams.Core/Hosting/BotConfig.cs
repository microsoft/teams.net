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
public sealed class BotConfig
{
    internal const string DefaultSectionName = "AzureAd";

    internal const string BotFrameworkSectionName = "BotFramework";

    internal const string DefaultOpenIdMetadataUrl = "https://login.botframework.com/v1/.well-known/openid-configuration";

    internal const string DefaultEntraInstance = "https://login.microsoftonline.com/";

    internal const string DefaultBotTokenIssuer = "https://api.botframework.com";

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

    /// <summary>
    /// Gets or sets the Bot Framework OpenID metadata URL used to fetch signing keys
    /// for validating inbound Bot Framework tokens. For sovereign clouds, set
    /// <c>BotFramework:OpenIdMetadataUrl</c> in configuration, e.g.
    /// <c>"https://login.botframework.azure.us/v1/.well-known/openid-configuration"</c> for USGov.
    /// Defaults to the public-cloud endpoint when not configured.
    /// </summary>
    public string OpenIdMetadataUrl { get; set; } = DefaultOpenIdMetadataUrl;

    /// <summary>
    /// Gets or sets the Entra login instance used when validating Entra-issued tokens.
    /// For sovereign clouds, set <c>{SectionName}:Instance</c> in configuration
    /// (the standard Microsoft.Identity.Web key), e.g.
    /// <c>"https://login.microsoftonline.us/"</c> for USGov.
    /// Defaults to the public-cloud instance when not configured.
    /// </summary>
    public string EntraInstance { get; set; } = DefaultEntraInstance;

    /// <summary>
    /// Gets or sets the expected Bot Framework token issuer used to validate inbound
    /// Bot Framework tokens. For sovereign clouds, set <c>BotFramework:BotTokenIssuer</c>
    /// in configuration, e.g. <c>"https://api.botframework.us"</c> for USGov.
    /// Defaults to the public-cloud issuer when not configured.
    /// </summary>
    public string BotTokenIssuer { get; set; } = DefaultBotTokenIssuer;

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

        IConfiguration? configuration = AddBotApplicationExtensions.ResolveFromServicesPreHost<IConfiguration>(services);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                "IConfiguration must be registered in the service collection before calling BotConfig.Resolve. " +
                "Ensure AddConfiguration() or WebApplication.CreateBuilder() has been called.");
        }

        ILogger logger = AddBotApplicationExtensions.GetLoggerFromServices(services, typeof(BotConfig));

        IConfigurationSection section = configuration.GetSection(sectionName);
        IConfigurationSection botFrameworkSection = configuration.GetSection(BotFrameworkSectionName);
        BotConfig config = new()
        {
            TenantId = section["TenantId"] ?? string.Empty,
            ClientId = section["ClientId"] ?? string.Empty,
            EntraInstance = ResolveAbsoluteUri(section, "Instance", DefaultEntraInstance),
            OpenIdMetadataUrl = ResolveAbsoluteUri(botFrameworkSection, "OpenIdMetadataUrl", DefaultOpenIdMetadataUrl),
            BotTokenIssuer = ResolveAbsoluteUri(botFrameworkSection, "BotTokenIssuer", DefaultBotTokenIssuer),
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

    private static string ResolveAbsoluteUri(IConfigurationSection section, string key, string defaultValue)
    {
        ArgumentNullException.ThrowIfNull(section);

        string? value = section[key];
        if (value is null)
        {
            return defaultValue;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"Configuration value '{section.Key}:{key}' is not a valid absolute URI: '{value}'.");
        }
        return value;
    }

    private static readonly Action<ILogger, string, Exception?> _logUsingSectionConfig =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Resolved bot configuration from '{SectionName}' configuration section");
    private static readonly Action<ILogger, Exception?> _logNoConfigFound =
        LoggerMessage.Define(LogLevel.Warning, new(4), "No bot configuration found in configuration.");

}
