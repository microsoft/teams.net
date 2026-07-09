// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Security.Claims;
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

namespace Microsoft.Teams.Core.Hosting
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication and authorization for bots and agents.
    /// </summary>
    public static class JwtExtensions
    {
        /// <summary>
        /// Adds JWT authentication for bots and agents using configuration from appsettings.
        /// </summary>
        /// <param name="services">The service collection to add authentication to.</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <param name="logger">The logger instance for logging.</param>
        /// <returns>An <see cref="AuthenticationBuilder"/> for further authentication configuration.</returns>
        public static AuthenticationBuilder AddBotAuthentication(this IServiceCollection services, string aadSectionName = BotConfig.DefaultSectionName, ILogger? logger = null)
        {
            BotConfig botConfig = BotConfig.Resolve(services, aadSectionName);
            return services.AddBotAuthentication(botConfig.ClientId, botConfig.TenantId, aadSectionName, logger);
        }

        /// <summary>
        /// Adds JWT authentication for bots and agents with manually provided configuration values.
        /// </summary>
        /// <param name="services">The service collection to add authentication to.</param>
        /// <param name="clientId">The application (client) ID for token validation.</param>
        /// <param name="tenantId">The Azure AD tenant ID. Can be empty for multi-tenant scenarios.</param>
        /// <param name="schemeName">The authentication scheme name. Defaults to "AzureAd".</param>
        /// <param name="logger">Optional logger instance for logging. If null, a NullLogger will be used.</param>
        /// <returns>An <see cref="AuthenticationBuilder"/> for further authentication configuration.</returns>
        public static AuthenticationBuilder AddBotAuthentication(
            this IServiceCollection services,
            string clientId,
            string tenantId = "",
            string schemeName = BotConfig.DefaultSectionName,
            ILogger? logger = null)
        {
            AuthenticationBuilder builder = services.AddAuthentication();
            builder.AddBotAuthentication(clientId, tenantId, schemeName, logger);
            return builder;
        }

        /// <summary>
        /// Adds JWT authentication for bots and agents to an existing authentication builder.
        /// Use this overload when registering multiple authentication schemes to avoid calling AddAuthentication() multiple times.
        /// </summary>
        /// <param name="builder">The existing authentication builder.</param>
        /// <param name="clientId">The application (client) ID for token validation.</param>
        /// <param name="tenantId">The Azure AD tenant ID. Can be empty for multi-tenant scenarios.</param>
        /// <param name="schemeName">The authentication scheme name.</param>
        /// <param name="logger">Optional logger instance for logging. If null, a NullLogger will be used.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/> for chaining.</returns>
        public static AuthenticationBuilder AddBotAuthentication(
            this AuthenticationBuilder builder,
            string clientId,
            string tenantId = "",
            string schemeName = BotConfig.DefaultSectionName,
            ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                builder.AddBypassAuthentication(schemeName, logger);
            }
            else
            {
                builder.AddTeamsJwtBearer(schemeName, clientId, tenantId, logger);
            }
            return builder;
        }

        /// <summary>
        /// Adds authorization policies to the service collection using configuration from appsettings.
        /// </summary>
        /// <param name="services">The service collection to add authorization to.</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <param name="logger">Optional logger instance for logging. If null, a NullLogger will be used.</param>
        /// <returns>An <see cref="AuthorizationBuilder"/> for further authorization configuration.</returns>
        public static AuthorizationBuilder AddBotAuthorization(this IServiceCollection services, string aadSectionName = BotConfig.DefaultSectionName, ILogger? logger = null)
        {
            logger ??= NullLogger.Instance;

            BotConfig botConfig = BotConfig.Resolve(services, aadSectionName);
            return services.AddBotAuthorization(botConfig, logger);
        }

        /// <summary>
        /// Adds authorization policies to the service collection using configuration from appsettings.
        /// </summary>
        /// <param name="services">The service collection to add authorization to.</param>
        /// <param name="botConfig">The bot configuration settings.</param>
        /// <param name="logger">Optional logger instance for logging. If null, a NullLogger will be used.</param>
        /// <returns>An <see cref="AuthorizationBuilder"/> for further authorization configuration.</returns>
        internal static AuthorizationBuilder AddBotAuthorization(this IServiceCollection services, BotConfig botConfig, ILogger? logger = null)
        {
            logger ??= NullLogger.Instance;

            // Call AddTeamsJwtBearer with the already-resolved BotConfig to avoid a redundant
            // BotConfig.Resolve call (and duplicate startup log) that would occur through the
            // public string-based AddBotAuthentication → AddTeamsJwtBearer chain.
            AuthenticationBuilder authBuilder = services.AddAuthentication();
            if (botConfig.DangerouslyAllowUnauthenticatedRequests || string.IsNullOrWhiteSpace(botConfig.ClientId))
            {
                authBuilder.AddBypassAuthentication(botConfig.SectionName, logger);
            }
            else
            {
                authBuilder.AddTeamsJwtBearer(botConfig, logger);
            }

            return services
                .AddAuthorizationBuilder()
                .AddDefaultPolicy(botConfig.SectionName, policy =>
                {
                    policy.AuthenticationSchemes.Add(botConfig.SectionName);
                    policy.RequireAuthenticatedUser();
                });
        }

        /// <summary>
        /// Adds authorization policies to the service collection with manually provided configuration values.
        /// </summary>
        /// <param name="services">The service collection to add authorization to.</param>
        /// <param name="clientId">The application (client) ID for token validation.</param>
        /// <param name="tenantId">The Azure AD tenant ID. Can be empty for multi-tenant scenarios.</param>
        /// <param name="schemeName">The authentication scheme name. Defaults to "AzureAd".</param>
        /// <param name="logger">Optional logger instance for logging. If null, a NullLogger will be used.</param>
        /// <returns>An <see cref="AuthorizationBuilder"/> for further authorization configuration.</returns>
        public static AuthorizationBuilder AddBotAuthorization(
            this IServiceCollection services,
            string clientId,
            string tenantId = "",
            string schemeName = BotConfig.DefaultSectionName,
            ILogger? logger = null)
        {
            services.AddBotAuthentication(clientId, tenantId, schemeName, logger);

            return services
                .AddAuthorizationBuilder()
                .AddDefaultPolicy(schemeName, policy =>
                {
                    policy.AuthenticationSchemes.Add(schemeName);
                    policy.RequireAuthenticatedUser();
                });
        }

        internal static string ValidateTeamsIssuer(string issuer, SecurityToken token, string configuredTenantId, string entraInstance, string botTokenIssuer)
        {
            // Bot Framework tokens. The expected issuer varies by sovereign cloud
            // (e.g. https://api.botframework.us for USGov) so it comes from configuration.
            if (issuer.Equals(botTokenIssuer, StringComparison.OrdinalIgnoreCase))
            {
                return issuer;
            }

            // Entra tokens - bot-to-bot (agent) and user (tab/API)
            // Use the token's own tid claim for multi-tenant; fall back to configured tenant.
            // The v2.0 expected issuer is derived from the configured Entra instance so sovereign
            // tokens (e.g. login.microsoftonline.us) validate correctly.
            (_, string? tid) = GetTokenClaims(token);
            string? effectiveTenant = string.IsNullOrEmpty(configuredTenantId) ? tid : configuredTenantId;

            if (effectiveTenant is not null &&
                (issuer == $"{entraInstance}{effectiveTenant}/v2.0" ||
                 issuer == $"https://sts.windows.net/{effectiveTenant}/"))
            {
                return issuer;
            }

            throw new SecurityTokenInvalidIssuerException(
                $"Issuer '{issuer}' is not valid for tenant '{effectiveTenant ?? "<unknown>"}'.");
        }

        /// <summary>
        /// Picks the OIDC metadata authority to fetch signing keys from based on the token's
        /// issuer claim. Tokens issued by the configured Bot Framework issuer (e.g. the public
        /// "https://api.botframework.com" or a sovereign equivalent like "https://api.botframework.us")
        /// resolve to the configured Bot OIDC URL; all others fall through to the Entra tenant authority.
        /// </summary>
        internal static string ResolveSigningAuthority(string? iss, string? tid, string botTokenIssuer, string botOidcUrl, string entraInstance)
        {
            if (iss is null) return string.Empty;
            return iss.Equals(botTokenIssuer, StringComparison.OrdinalIgnoreCase)
                ? botOidcUrl
                : $"{entraInstance}{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";
        }

        private static (string? iss, string? tid) GetTokenClaims(SecurityToken token) =>
            token is JsonWebToken jwt
                ? (jwt.Issuer, jwt.TryGetClaim("tid", out Claim? c) ? c.Value : null)
                : (null, null);

        /// <summary>
        /// Overload that accepts an already-resolved <see cref="BotConfig"/> to avoid a redundant
        /// <see cref="BotConfig.Resolve"/> call during internal registration paths.
        /// </summary>
        private static AuthenticationBuilder AddTeamsJwtBearer(this AuthenticationBuilder builder, BotConfig botConfig, ILogger? logger = null)
        {
            return builder.AddTeamsJwtBearer(
                botConfig.SectionName,
                botConfig.ClientId,
                botConfig.TenantId,
                botConfig.OpenIdMetadataUrl,
                botConfig.EntraInstance,
                botConfig.BotTokenIssuer,
                logger);
        }

        private static AuthenticationBuilder AddTeamsJwtBearer(this AuthenticationBuilder builder, string schemeName, string audience, string tenantId, ILogger? logger = null)
        {
            // Resolve sovereign-cloud-aware URLs from the same AzureAd section that produced clientId/tenantId.
            // Defaults to the public-cloud values when IConfiguration is not registered (manual-credentials callers)
            // or when the section is missing or doesn't override them.
            string botOidcUrl = BotConfig.DefaultOpenIdMetadataUrl;
            string entraInstance = BotConfig.DefaultEntraInstance;
            string botTokenIssuer = BotConfig.DefaultBotTokenIssuer;
            if (builder.Services.Any(d => d.ServiceType == typeof(IConfiguration)))
            {
                BotConfig botConfig = BotConfig.Resolve(builder.Services, schemeName);
                botOidcUrl = botConfig.OpenIdMetadataUrl;
                entraInstance = botConfig.EntraInstance;
                botTokenIssuer = botConfig.BotTokenIssuer;
            }

            return builder.AddTeamsJwtBearer(schemeName, audience, tenantId, botOidcUrl, entraInstance, botTokenIssuer, logger);
        }

        private static AuthenticationBuilder AddTeamsJwtBearer(
            this AuthenticationBuilder builder,
            string schemeName,
            string audience,
            string tenantId,
            string botOidcUrl,
            string entraInstance,
            string botTokenIssuer,
            ILogger? logger)
        {

            // One ConfigurationManager per OIDC authority, shared safely across all requests.
            ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> configManagerCache = new(StringComparer.OrdinalIgnoreCase);

            // Cache resolved configurations to avoid blocking async calls on every token validation.
            // ConfigurationManager handles background refresh internally; we cache the Task so that
            // only the first call per authority actually blocks.
            ConcurrentDictionary<string, Task<OpenIdConnectConfiguration>> configCache = new(StringComparer.OrdinalIgnoreCase);

            builder.AddJwtBearer(schemeName, jwtOptions =>
            {
                jwtOptions.SaveToken = true;
                jwtOptions.IncludeErrorDetails = true;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudiences = [audience, $"api://{audience}"],
                    IssuerValidator = (issuer, token, _) => ValidateTeamsIssuer(issuer, token, tenantId, entraInstance, botTokenIssuer),
                    IssuerSigningKeyResolver = (_, securityToken, _, _) =>
                    {
                        (string? iss, string? tid) = GetTokenClaims(securityToken);
                        if (iss is null) return [];

                        string authority = ResolveSigningAuthority(iss, tid, botTokenIssuer, botOidcUrl, entraInstance);

                        logger?.ResolvingSigningKeys(authority, iss);

                        ConfigurationManager<OpenIdConnectConfiguration> manager = configManagerCache.GetOrAdd(authority, a =>
                            new ConfigurationManager<OpenIdConnectConfiguration>(
                                a,
                                new OpenIdConnectConfigurationRetriever(),
                                new HttpDocumentRetriever { RequireHttps = jwtOptions.RequireHttpsMetadata }));

                        // Cache the Task so only the first call per authority blocks;
                        // subsequent calls return the already-completed task synchronously.
                        // ConfigurationManager handles background refresh of stale configs internally.
                        Task<OpenIdConnectConfiguration> configTask = configCache.GetOrAdd(authority,
                            _ => manager.GetConfigurationAsync(CancellationToken.None));

                        OpenIdConnectConfiguration config = configTask.ConfigureAwait(false).GetAwaiter().GetResult();
                        return config.SigningKeys;
                    }
                };
                jwtOptions.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
                jwtOptions.MapInboundClaims = true;
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        ILogger log = GetLogger(context.HttpContext, logger);
                        log.TokenValidated(schemeName);
                        if (log.IsEnabled(LogLevel.Trace) && context.SecurityToken is JsonWebToken jwt)
                        {
                            string claims = Environment.NewLine + string.Join(Environment.NewLine, jwt.Claims.Select(c => $"  {c.Type}: {c.Value}"));
                            log.IncomingTokenClaims(claims);
                        }
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        GetLogger(context.HttpContext, logger).ForbiddenForScheme(schemeName);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        ILogger log = GetLogger(context.HttpContext, logger);

                        string? tokenIssuer = null;
                        string? tokenAudience = null;
                        string? tokenExpiration = null;
                        string? tokenSubject = null;
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
                        string expectedAudiences = validationParams?.ValidAudiences is not null
                            ? string.Join(", ", validationParams.ValidAudiences)
                            : validationParams?.ValidAudience ?? "n/a";
                        log.JwtAuthenticationFailed(
                            context.Exception,
                            schemeName,
                            context.Exception.Message,
                            tokenIssuer ?? "n/a",
                            tokenAudience ?? "n/a",
                            tokenExpiration ?? "n/a",
                            tokenSubject ?? "n/a",
                            expectedAudiences);

                        return Task.CompletedTask;
                    }
                };
                jwtOptions.Validate();
            });
            return builder;
        }

        private static AuthenticationBuilder AddBypassAuthentication(this AuthenticationBuilder builder, string schemeName, ILogger? logger = null)
        {
            (logger ?? NullLogger.Instance).BypassAuthenticationConfigured(schemeName);

            builder.AddJwtBearer(schemeName, jwtOptions =>
            {
#pragma warning disable CA5404 // Do not disable token validation checks
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens = false,
                    SignatureValidator = (token, _) => new JsonWebToken(token)
                };
#pragma warning restore CA5404 // Do not disable token validation checks
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Always succeed authentication even without a token
                        GetLogger(context.HttpContext, logger).BypassAuthenticationSucceeded(schemeName);
                        context.NoResult();
                        context.Principal = new System.Security.Claims.ClaimsPrincipal(
                            new System.Security.Claims.ClaimsIdentity("BypassAuth"));
                        context.Success();
                        return Task.CompletedTask;
                    }
                };
            });
            return builder;
        }

        private static ILogger GetLogger(HttpContext context, ILogger? fallback) =>
            context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
            ?? fallback
            ?? NullLogger.Instance;
    }
}
