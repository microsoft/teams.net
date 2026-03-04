// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Teams.Bot.Core.Hosting
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication and authorization for bots and agents.
    /// </summary>
    public static class JwtExtensions
    {
        internal const string TeamsScheme = "TeamsScheme";
        internal const string BotOIDC = "https://login.botframework.com/v1/.well-known/openid-configuration";
        internal const string EntraOIDC = "https://login.microsoftonline.com/";

        /// <summary>
        /// Adds JWT authentication for bots and agents.
        /// </summary>
        /// <param name="services">The service collection to add authentication to.</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <param name="logger">The logger instance for logging.</param>
        /// <returns>An <see cref="AuthenticationBuilder"/> for further authentication configuration.</returns>
        public static AuthenticationBuilder AddBotAuthentication(this IServiceCollection services, ILogger logger, string aadSectionName = "AzureAd")
        {
            AuthenticationBuilder builder = services.AddAuthentication();

            ServiceDescriptor? configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
            IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
                ?? services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            BotConfig botConfig = BotConfig.Resolve(configuration, aadSectionName);

            string schemeName = $"{TeamsScheme}_{aadSectionName}";
            builder.AddTeamsJwtBearer(schemeName, botConfig.ClientId, botConfig.TenantId, logger);

            return builder;
        }

        /// <summary>
        /// Adds authorization policies to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add authorization to.</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <param name="logger">Optional logger instance for logging. If null, a NullLogger will be used.</param>
        /// <returns>An <see cref="AuthorizationBuilder"/> for further authorization configuration.</returns>
        public static AuthorizationBuilder AddAuthorization(this IServiceCollection services, ILogger? logger = null, string aadSectionName = "AzureAd")
        {
            logger ??= NullLogger.Instance;

            services.AddBotAuthentication(logger, aadSectionName);

            return services
                .AddAuthorizationBuilder()
                .AddDefaultPolicy(aadSectionName, policy =>
                {
                    policy.AuthenticationSchemes.Add($"{TeamsScheme}_{aadSectionName}");
                    policy.RequireAuthenticatedUser();
                });
        }

        private static string ValidateTeamsIssuer(string issuer, SecurityToken token, string configuredTenantId)
        {
            // Bot Framework tokens
            if (issuer.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase))
                return issuer;

            // Entra tokens — bot-to-bot (agent) and user (tab/API)
            // Use the token's own tid claim for multi-tenant; fall back to configured tenant
            (_, string? tid) = GetTokenClaims(token);
            string? effectiveTenant = string.IsNullOrEmpty(configuredTenantId) ? tid : configuredTenantId;

            if (effectiveTenant is not null &&
                (issuer == $"https://login.microsoftonline.com/{effectiveTenant}/v2.0" ||
                 issuer == $"https://sts.windows.net/{effectiveTenant}/"))
                return issuer;

            throw new SecurityTokenInvalidIssuerException($"Issuer '{issuer}' is not valid.");
        }

        private static (string? iss, string? tid) GetTokenClaims(SecurityToken token) =>
            token is JsonWebToken jwt
                ? (jwt.Issuer, jwt.TryGetClaim("tid", out var c) ? c.Value : null)
                : (null, null);

        private static AuthenticationBuilder AddTeamsJwtBearer(this AuthenticationBuilder builder, string schemeName, string audience, string tenantId, ILogger? logger)
        {
            // One ConfigurationManager per OIDC authority, shared safely across all requests.
            ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> configManagerCache = new(StringComparer.OrdinalIgnoreCase);

            builder.AddJwtBearer(schemeName, jwtOptions =>
            {
                jwtOptions.SaveToken = true;
                jwtOptions.IncludeErrorDetails = true;
                jwtOptions.Audience = audience;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    IssuerValidator = (issuer, token, _) => ValidateTeamsIssuer(issuer, token, tenantId),
                    IssuerSigningKeyResolver = (_, securityToken, _, _) =>
                    {
                        (string? iss, string? tid) = GetTokenClaims(securityToken);
                        if (iss is null) return [];

                        string authority = iss.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                            ? BotOIDC
                            : $"{EntraOIDC}{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";

                        ConfigurationManager<OpenIdConnectConfiguration> manager = configManagerCache.GetOrAdd(authority, a =>
                            new ConfigurationManager<OpenIdConnectConfiguration>(
                                a,
                                new OpenIdConnectConfigurationRetriever(),
                                new HttpDocumentRetriever { RequireHttps = jwtOptions.RequireHttpsMetadata }));

                        OpenIdConnectConfiguration config = manager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
                        return config.SigningKeys;
                    }
                };
                jwtOptions.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
                jwtOptions.MapInboundClaims = true;
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        GetLogger(context.HttpContext, logger).LogDebug("Token validated for scheme: {Scheme}", schemeName);
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        GetLogger(context.HttpContext, logger).LogWarning("Forbidden for scheme: {Scheme}", schemeName);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        ILogger log = GetLogger(context.HttpContext, logger);

                        string? tokenIssuer = null, tokenAudience = null, tokenExpiration = null, tokenSubject = null;
                        string authHeader = context.Request.Headers.Authorization.ToString();
                        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                JsonWebToken jwt = new(authHeader["Bearer ".Length..].Trim());
                                (tokenIssuer, _) = GetTokenClaims(jwt);
                                tokenAudience = jwt.GetClaim("aud")?.Value;
                                tokenExpiration = jwt.ValidTo.ToString("o");
                                tokenSubject = jwt.Subject;
                            }
                            catch (ArgumentException) { }
                        }

                        TokenValidationParameters? validationParams = context.Options?.TokenValidationParameters;
                        log.LogError(context.Exception,
                            "JWT authentication failed for scheme {Scheme}: {ExceptionMessage} | " +
                            "token iss={TokenIssuer} aud={TokenAudience} exp={TokenExpiration} sub={TokenSubject} | " +
                            "expected aud={ConfiguredAudience}",
                            schemeName,
                            context.Exception.Message,
                            tokenIssuer ?? "n/a",
                            tokenAudience ?? "n/a",
                            tokenExpiration ?? "n/a",
                            tokenSubject ?? "n/a",
                            validationParams?.ValidAudience ?? "n/a");

                        return Task.CompletedTask;
                    }
                };
                jwtOptions.Validate();
            });
            return builder;
        }

        private static ILogger GetLogger(HttpContext context, ILogger? fallback) =>
            context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
            ?? fallback
            ?? NullLogger.Instance;
    }
}
