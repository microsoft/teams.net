// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Teams.Bot.Core.Hosting;


internal sealed class BotConfig
{
    public const string SystemManagedIdentityIdentifier = "system";

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
