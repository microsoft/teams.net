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

    internal const string LegacySectionName = "Teams";
    private const string DangerouslyAllowUnauthenticatedRequestsKey = "DangerouslyAllowUnauthenticatedRequests";

    internal const string DefaultOpenIdMetadataUrl = "https://login.botframework.com/v1/.well-known/openid-configuration";

    internal const string DefaultEntraInstance = "https://login.microsoftonline.com/";

    internal const string DefaultBotTokenIssuer = "https://api.botframework.com";

    internal const string DefaultGraphBaseUrl = "https://graph.microsoft.com";

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

    /// <summary>
    /// Gets or sets the Microsoft Graph host root that Graph-backed features address.
    /// For sovereign clouds, set <c>BotFramework:GraphBaseUrl</c> in configuration, e.g. <c>"https://graph.microsoft.us"</c> for USGov.
    /// Defaults to the public-cloud host when not configured.
    /// <para>A host root, not a versioned endpoint: callers append their own API version, so a pre-versioned value
    /// produces <c>/v1.0/v1.0</c>.</para>
    /// </summary>
    public string GraphBaseUrl { get; set; } = DefaultGraphBaseUrl;

    /// <summary>
    /// Gets or sets whether inbound bot requests should bypass authentication.
    /// This should only be enabled for local development.
    /// </summary>
    public bool DangerouslyAllowUnauthenticatedRequests { get; set; }

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
        return Resolve(services, sectionName, log: true);
    }

    internal static BotConfig Resolve(IServiceCollection services, string sectionName, bool log)
    {
        ArgumentNullException.ThrowIfNull(services);

        IConfiguration? configuration = AddBotApplicationExtensions.ResolveFromServicesPreHost<IConfiguration>(services);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                "IConfiguration must be registered in the service collection before calling BotConfig.Resolve. " +
                "Ensure AddConfiguration() or WebApplication.CreateBuilder() has been called.");
        }

        IConfigurationSection section = configuration.GetSection(sectionName);
        IConfigurationSection botFrameworkSection = configuration.GetSection(BotFrameworkSectionName);
        bool usingLegacyTeamsSection = false;

        // Backward compat: if the primary section has no ClientId, fall back to the legacy "Teams" section
        // and remap it to the AzureAd shape so all downstream code sees a consistent configuration.
        if (string.IsNullOrEmpty(section["ClientId"]))
        {
            IConfigurationSection teamsSection = configuration.GetSection(LegacySectionName);
            if (!string.IsNullOrEmpty(teamsSection["ClientId"]))
            {
                section = MapLegacyTeamsSection(teamsSection, sectionName);
                usingLegacyTeamsSection = true;
            }
        }

        bool dangerouslyAllowUnauthenticatedRequests =
            ResolveOptionalBoolean(section, DangerouslyAllowUnauthenticatedRequestsKey)
            ?? false;
        BotConfig config = new()
        {
            TenantId = section["TenantId"] ?? string.Empty,
            ClientId = section["ClientId"] ?? string.Empty,
            EntraInstance = ResolveAbsoluteUri(section, "Instance", DefaultEntraInstance),
            OpenIdMetadataUrl = ResolveAbsoluteUri(botFrameworkSection, "OpenIdMetadataUrl", DefaultOpenIdMetadataUrl),
            BotTokenIssuer = ResolveAbsoluteUri(botFrameworkSection, "BotTokenIssuer", DefaultBotTokenIssuer),
            GraphBaseUrl = ResolveAbsoluteUri(botFrameworkSection, "GraphBaseUrl", DefaultGraphBaseUrl),
            DangerouslyAllowUnauthenticatedRequests = dangerouslyAllowUnauthenticatedRequests,
            MsalConfigurationSection = section,
            SectionName = sectionName
        };

        if (log)
        {
            AddBotApplicationExtensions.LogFromServices(services, l =>
            {
                if (usingLegacyTeamsSection)
                    _logUsingLegacySection(l, LegacySectionName, sectionName, BuildCurrentSectionExample(sectionName), null);

                if (config.DangerouslyAllowUnauthenticatedRequests)
                    l.BypassAuthenticationConfigured(sectionName);
                else if (!string.IsNullOrEmpty(config.ClientId))
                    _logUsingSectionConfig(l, sectionName, null);
                else
                    l.AuthenticationNotConfigured(sectionName);

                if (!GraphHostMatchesCloud(config.EntraInstance, config.GraphBaseUrl))
                    l.GraphHostMayNotMatchCloud(config.GraphBaseUrl, config.EntraInstance, sectionName);
            }, typeof(BotConfig));
        }

        return config;
    }

    private static IConfigurationSection MapLegacyTeamsSection(IConfigurationSection teams, string targetSection) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{targetSection}:TenantId"]                          = teams["TenantId"],
                [$"{targetSection}:ClientId"]                          = teams["ClientId"],
                [$"{targetSection}:Instance"]                          = DefaultEntraInstance,
                [$"{targetSection}:{DangerouslyAllowUnauthenticatedRequestsKey}"] = teams[DangerouslyAllowUnauthenticatedRequestsKey],
                [$"{targetSection}:ClientCredentials:0:SourceType"]    = "ClientSecret",
                [$"{targetSection}:ClientCredentials:0:ClientSecret"]  = teams["ClientSecret"],
            })
            .Build()
            .GetSection(targetSection);

    private static string BuildCurrentSectionExample(string sectionName) =>
        $$"""
              {
          "{{sectionName}}": {
            "Instance": "https://login.microsoftonline.com/",
            "TenantId": "your-tenant-id",
            "ClientId": "your-client-id",
            "ClientCredentials": [
              {
                "SourceType": "ClientSecret",
                "ClientSecret": "your-client-secret"
              }
            ]
          }
        }
        """;

    private static bool? ResolveOptionalBoolean(IConfigurationSection section, string key)
    {
        ArgumentNullException.ThrowIfNull(section);

        string? value = section[key];
        if (value is null)
        {
            return null;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        throw new InvalidOperationException(
            $"Configuration value '{section.Path}:{key}' is not a valid boolean: '{value}'.");
    }
    /// <summary>
    /// Whether a Graph host is one this SDK associates with the Entra instance the rest of the configuration implies.
    /// </summary>
    /// <remarks>
    /// <para>A derived Graph host cannot disagree with the cloud, but a configuration key can, and the two are set independently. The failure this catches is the quiet one: an operator sets <c>Instance</c> for a sovereign cloud, leaves <c>GraphBaseUrl</c> alone, and silently inherits the public-cloud default until the first file download returns 401 from the wrong cloud.</para>
    /// <para>Unknown instances return <c>true</c> deliberately. Air-gapped and future clouds are not enumerable here, and a warning aimed at a deployment this SDK knows nothing about would be noise rather than a signal.</para>
    /// <para>The US Gov instance maps to two Graph hosts because GCC High and DoD share an Entra endpoint and differ only in their Graph resource.</para>
    /// </remarks>
    internal static bool GraphHostMatchesCloud(string entraInstance, string graphBaseUrl)
    {
        if (!Uri.TryCreate(entraInstance, UriKind.Absolute, out Uri? entra)
            || !Uri.TryCreate(graphBaseUrl, UriKind.Absolute, out Uri? graph))
        {
            return true;
        }

        static bool IsHost(Uri uri, string host) => string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase);

        string[]? expected =
            IsHost(entra, "login.microsoftonline.com") ? ["graph.microsoft.com"]
            : IsHost(entra, "login.microsoftonline.us") ? ["graph.microsoft.us", "dod-graph.microsoft.us"]
            : IsHost(entra, "login.partner.microsoftonline.cn") ? ["microsoftgraph.chinacloudapi.cn"]
            : null;

        return expected is null || expected.Contains(graph.Host, StringComparer.OrdinalIgnoreCase);
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
    private static readonly Action<ILogger, string, string, string, Exception?> _logUsingLegacySection =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new(5),
            "Configuration section '{LegacySectionName}' is deprecated. Please migrate to '{CurrentSectionName}' using the Microsoft.Identity.Web configuration structure:\n{CurrentSectionExample}");
}
