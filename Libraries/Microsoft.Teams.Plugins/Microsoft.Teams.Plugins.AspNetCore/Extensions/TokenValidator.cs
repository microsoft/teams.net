using System.Collections.Concurrent;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class TokenValidator
{
    private static readonly ConcurrentDictionary<string, IConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache = new();

    // Add more options to configure other token types
    public static void ConfigureValidation(JwtBearerOptions options, IEnumerable<string> validIssuers, IEnumerable<string> validAudiences,
        string? openIdMetadataUrl = null)
    {
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = validIssuers.Any(),
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidIssuers = validIssuers,
            ValidAudiences = validAudiences,
        };

        // stricter validation: ensures the key’s issuer matches the token issuer
        options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();

        // use cached OpenID Connect metadata
        if (openIdMetadataUrl != null)
        {
            options.ConfigurationManager = _openIdMetadataCache.GetOrAdd(
                openIdMetadataUrl,
                key => new ConfigurationManager<OpenIdConnectConfiguration>(
                openIdMetadataUrl, new OpenIdConnectConfigurationRetriever(), new HttpClient())
                {
                    AutomaticRefreshInterval = BaseConfigurationManager.DefaultAutomaticRefreshInterval
                });
        }
    }
}