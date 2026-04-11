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

namespace Microsoft.Teams.Bot.Core.Hosting
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication and authorization for bots and agents.
    /// </summary>
    public static class JwtExtensions
    {
        internal const string BotOIDC = "https://login.botframework.com/v1/.well-known/openid-configuration";
        internal const string EntraOIDC = "https://login.microsoftonline.com/";

        /// <summary>
        /// Adds JWT authentication for bots and agents using configuration from appsettings.
        /// </summary>
        /// <param name="services">The service collection to add authentication to.</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <param name="logger">The logger instance for logging.</param>
        /// <returns>An <see cref="AuthenticationBuilder"/> for further authentication configuration.</returns>
        public static AuthenticationBuilder AddBotAuthentication(this IServiceCollection services, string aadSectionName = "AzureAd", ILogger? logger = null)
        {
            BotConfig botConfig = ResolveBotConfig(services, aadSectionName);
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
            string schemeName = "AzureAd",
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
            string schemeName = "AzureAd",
            ILogger? logger = null)
        {
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
        public static AuthorizationBuilder AddBotAuthorization(this IServiceCollection services, string aadSectionName = "AzureAd", ILogger? logger = null)
        {
            logger ??= NullLogger.Instance;

            BotConfig botConfig = ResolveBotConfig(services, aadSectionName);
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

            return services.AddBotAuthorization(botConfig.ClientId, botConfig.TenantId, botConfig.SectionName, logger);
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
            string schemeName = "AzureAd",
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

        private static string ValidateTeamsIssuer(string issuer, SecurityToken token, string configuredTenantId)
        {
            // Bot Framework tokens
            if (issuer.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase))
                return issuer;

            // Entra tokens � bot-to-bot (agent) and user (tab/API)
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
                ? (jwt.Issuer, jwt.TryGetClaim("tid", out Claim? c) ? c.Value : null)
                : (null, null);

        /// <summary>
        /// Adds Teams JWT Bearer authentication that supports both Bot Framework and Entra ID tokens.
        /// </summary>
        /// <param name="builder">The authentication builder.</param>
        /// <param name="schemeName">The authentication scheme name.</param>
        /// <param name="audience">The application (client) ID used to validate the audience of tokens.</param>
        /// <param name="tenantId">The Azure AD tenant ID.</param>
        /// <param name="logger">Optional logger for authentication events.</param>
        /// <returns>The authentication builder for chaining.</returns>
        /// <remarks>
        /// This method configures authentication to support both types of tokens:
        /// <list type="bullet">
        /// <item><description>Bot Framework tokens: Issued by the Bot Connector service when channels send activities to your bot (issuer: https://api.botframework.com).</description></item>
        /// <item><description>Entra ID tokens: Issued by Azure AD when the bot is registered as an agentic application (issuer: https://login.microsoftonline.com). See https://learn.microsoft.com/en-us/microsoft-agent-365/developer/identity#understanding-agent-identity-components</description></item>
        /// </list>
        /// The signing keys for both token types are dynamically resolved at runtime using OpenID Connect discovery,
        /// allowing the same authentication configuration to validate tokens from multiple issuers.
        /// </remarks>
        private static AuthenticationBuilder AddTeamsJwtBearer(this AuthenticationBuilder builder, string schemeName, string audience, string tenantId, ILogger? logger = null)
        {
            // One JwksKeyCache per OIDC authority. The cache pre-warms in the background so the
            // synchronous IssuerSigningKeyResolver can almost always serve from a volatile field
            // without blocking any threads. Blocking only occurs on the very first request for a
            // new authority before the background fetch has completed (cold start).
            ConcurrentDictionary<string, JwksKeyCache> keyCacheByAuthority = new(StringComparer.OrdinalIgnoreCase);

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
                    IssuerValidator = (issuer, token, _) => ValidateTeamsIssuer(issuer, token, tenantId),
                    IssuerSigningKeyResolver = (_, securityToken, _, _) =>
                    {
                        (string? iss, string? tid) = GetTokenClaims(securityToken);
                        if (iss is null) return [];

                        string authority = iss.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                            ? BotOIDC
                            : $"{EntraOIDC}{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";

                        JwksKeyCache cache = keyCacheByAuthority.GetOrAdd(authority, a =>
                            new JwksKeyCache(a, jwtOptions.RequireHttpsMetadata));

                        return cache.GetKeys();
                    }
                };
                jwtOptions.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
                jwtOptions.MapInboundClaims = true;
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        GetLogger(context.HttpContext, logger).LogTraceGuarded("Token validated for scheme: {Scheme}", schemeName);
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
            (logger ?? NullLogger.Instance).LogWarning("ClientId not provided for scheme '{SchemeName}'. Configuring bypass authentication (no token validation). This is INSECURE and should only be used for development.", schemeName);

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
                        GetLogger(context.HttpContext, logger).LogWarning("Using bypass authentication scheme succeeded for scheme: {Scheme}. This is INSECURE and should only be used for development.", schemeName);
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

        private static BotConfig ResolveBotConfig(IServiceCollection services, string sectionName)
        {
            ServiceDescriptor? configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
            IConfiguration configuration = configDescriptor?.ImplementationInstance as IConfiguration
                ?? services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            return BotConfig.Resolve(configuration, sectionName);
        }

        private static ILogger GetLogger(HttpContext context, ILogger? fallback) =>
            context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
            ?? fallback
            ?? NullLogger.Instance;
    }

    /// <summary>
    /// Maintains a background-refreshed cache of OIDC signing keys for a single authority so that
    /// the synchronous <see cref="TokenValidationParameters.IssuerSigningKeyResolver"/> delegate
    /// can serve keys without blocking any thread-pool thread on the hot request path.
    ///
    /// On the very first call (cold start), before the background fetch has completed, the method
    /// falls back to a blocking fetch executed on a dedicated thread-pool thread to avoid capturing
    /// any ambient <see cref="System.Threading.SynchronizationContext"/> and preventing deadlocks.
    /// </summary>
    internal sealed class JwksKeyCache
    {
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _manager;

        // Written only by RefreshAsync; read by GetKeys() on every request.
        // Volatile ensures reads see the latest write without a lock.
        private volatile IReadOnlyList<SecurityKey> _cached = [];

        internal JwksKeyCache(string authority, bool requireHttps)
        {
            _manager = new ConfigurationManager<OpenIdConnectConfiguration>(
                authority,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = requireHttps });

            // Kick off the first fetch in the background; do not block the startup path.
            _ = Task.Run(() => RefreshAsync(CancellationToken.None));
        }

        // Overload for testing: allows injecting a pre-built ConfigurationManager.
        internal JwksKeyCache(ConfigurationManager<OpenIdConnectConfiguration> manager)
        {
            _manager = manager;
            _ = Task.Run(() => RefreshAsync(CancellationToken.None));
        }

        /// <summary>
        /// Returns cached signing keys. On a warm cache this is always allocation-free and
        /// non-blocking. On a cold cache (background fetch not yet complete) it blocks briefly
        /// on a pool thread to avoid sync-context deadlocks.
        /// </summary>
        internal IEnumerable<SecurityKey> GetKeys()
        {
            IReadOnlyList<SecurityKey> snapshot = _cached;
            if (snapshot.Count > 0)
                return snapshot;

            // Cold path: background warm-up has not completed yet.
            // Task.Run avoids capturing any ambient SynchronizationContext.
            return Task.Run(() => RefreshAsync(CancellationToken.None)).GetAwaiter().GetResult();
        }

        private async Task<IReadOnlyList<SecurityKey>> RefreshAsync(CancellationToken ct)
        {
            OpenIdConnectConfiguration config = await _manager.GetConfigurationAsync(ct).ConfigureAwait(false);
            IReadOnlyList<SecurityKey> fresh = [.. config.SigningKeys];
            _cached = fresh;
            return fresh;
        }
    }
}
