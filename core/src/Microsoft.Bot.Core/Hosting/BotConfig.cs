// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Core.Hosting;


internal class BotConfig
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
