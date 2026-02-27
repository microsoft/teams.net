// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        internal const string BotScheme = "BotScheme";
        internal const string AgentScheme = "AgentScheme";
        internal const string BotScope = "https://api.botframework.com/.default";
        internal const string AgentScope = "https://botapi.skype.com/.default";
        internal const string BotOIDC = "https://login.botframework.com/v1/.well-known/openid-configuration";
        internal const string AgentOIDC = "https://login.microsoftonline.com/";

        /// <summary>
        /// Adds JWT authentication for bots and agents.
        /// </summary>
        /// <param name="services">The service collection to add authentication to.</param>
        /// <param name="configuration">The application configuration containing the settings.</param>
        /// <param name="useAgentAuth">Indicates whether to use agent authentication (true) or bot authentication (false).</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <param name="logger">The logger instance for logging.</param>
        /// <returns>An <see cref="AuthenticationBuilder"/> for further authentication configuration.</returns>
        public static AuthenticationBuilder AddBotAuthentication(this IServiceCollection services, IConfiguration configuration, bool useAgentAuth, ILogger logger, string aadSectionName = "AzureAd")
        {

            // TODO: Task 5039187: Refactor use of BotConfig for MSAL and JWT

            AuthenticationBuilder builder = services.AddAuthentication();
            ArgumentNullException.ThrowIfNull(configuration);
            string audience = configuration[$"{aadSectionName}:ClientId"]
                   ?? configuration["CLIENT_ID"]
                   ?? configuration["MicrosoftAppId"]
                   ?? throw new InvalidOperationException("ClientID not found in configuration, tried the 3 option");

            if (!useAgentAuth)
            {
                string[] validIssuers = ["https://api.botframework.com"];
                builder.AddCustomJwtBearer($"BotScheme_{aadSectionName}", validIssuers, audience, logger);
            }
            else
            {
                string tenantId = configuration[$"{aadSectionName}:TenantId"]
                    ?? configuration["TENANT_ID"]
                    ?? configuration["MicrosoftAppTenantId"]
                    ?? "botframework.com"; // TODO: Task 5039198: Test JWT Validation for MultiTenant

                string[] validIssuers = [$"https://sts.windows.net/{tenantId}/", $"https://login.microsoftonline.com/{tenantId}/v2", "https://api.botframework.com"];
                builder.AddCustomJwtBearer(AgentScheme, validIssuers, audience, logger);
            }
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
            // Use NullLogger if no logger provided
            logger ??= Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            // We need IConfiguration to determine which authentication scheme to register (Bot vs Agent)
            // This is a registration-time decision that cannot be deferred
            // Try to get it from service descriptors first (fast path)
            ServiceDescriptor? configDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
            IConfiguration? configuration = configDescriptor?.ImplementationInstance as IConfiguration;

            // If not available as ImplementationInstance, build a temporary ServiceProvider
            // NOTE: This is generally an anti-pattern, but acceptable here because:
            // 1. We need configuration at registration time to select auth scheme
            // 2. We properly dispose the temporary ServiceProvider immediately
            // 3. This only happens once during application startup
            if (configuration == null)
            {
                using ServiceProvider tempProvider = services.BuildServiceProvider();
                configuration = tempProvider.GetRequiredService<IConfiguration>();
            }

            string? azureScope = configuration["Scope"];
            bool useAgentAuth = string.Equals(azureScope, AgentScope, StringComparison.OrdinalIgnoreCase);

            services.AddBotAuthentication(configuration, useAgentAuth, logger, aadSectionName);
            AuthorizationBuilder authorizationBuilder = services
                .AddAuthorizationBuilder()
                .AddDefaultPolicy(aadSectionName, policy =>
                {
                    if (!useAgentAuth)
                    {
                        policy.AuthenticationSchemes.Add($"BotScheme_{aadSectionName}");
                    }
                    else
                    {
                        policy.AuthenticationSchemes.Add(AgentScheme);
                    }
                    policy.RequireAuthenticatedUser();
                });
            return authorizationBuilder;
        }

        private static AuthenticationBuilder AddCustomJwtBearer(this AuthenticationBuilder builder, string schemeName, string[] validIssuers, string audience, ILogger? logger)
        {
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
                    ValidIssuers = validIssuers
                };
                jwtOptions.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
                jwtOptions.MapInboundClaims = true;
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        // Resolve logger at runtime from request services to ensure we always have proper logging
                        ILoggerFactory? loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        ILogger requestLogger = loggerFactory?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
                            ?? logger
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

                        requestLogger.LogDebug("OnMessageReceived invoked for scheme: {Scheme}", schemeName);
                        string authorizationHeader = context.Request.Headers.Authorization.ToString();

                        if (string.IsNullOrEmpty(authorizationHeader))
                        {
                            // Default to AadTokenValidation handling
                            context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                            await Task.CompletedTask.ConfigureAwait(false);
                            requestLogger.LogWarning("Authorization header is missing for scheme: {Scheme}", schemeName);
                            return;
                        }

                        string[] parts = authorizationHeader?.Split(' ')!;
                        if (parts.Length != 2 || parts[0] != "Bearer")
                        {
                            // Default to AadTokenValidation handling
                            context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                            await Task.CompletedTask.ConfigureAwait(false);
                            requestLogger.LogWarning("Invalid authorization header format for scheme: {Scheme}", schemeName);
                            return;
                        }

                        JwtSecurityToken token = new(parts[1]);
                        string issuer = token.Claims.FirstOrDefault(claim => claim.Type == "iss")?.Value!;
                        string tid = token.Claims.FirstOrDefault(claim => claim.Type == "tid")?.Value!;

                        string oidcAuthority = issuer.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                            ? BotOIDC : $"{AgentOIDC}{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";

                        requestLogger.LogDebug("Using OIDC Authority: {OidcAuthority} for issuer: {Issuer}", oidcAuthority, issuer);

                        jwtOptions.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                            oidcAuthority,
                            new OpenIdConnectConfigurationRetriever(),
                            new HttpDocumentRetriever
                            {
                                RequireHttps = jwtOptions.RequireHttpsMetadata
                            });


                        await Task.CompletedTask.ConfigureAwait(false);
                    },
                    OnTokenValidated = context =>
                    {
                        // Resolve logger at runtime
                        ILoggerFactory? loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        ILogger requestLogger = loggerFactory?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
                            ?? logger
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

                        requestLogger.LogInformation("Token validated successfully for scheme: {Scheme}", schemeName);
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        // Resolve logger at runtime
                        ILoggerFactory? loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        ILogger requestLogger = loggerFactory?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
                            ?? logger
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

                        requestLogger.LogWarning("Forbidden response for scheme: {Scheme}", schemeName);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Resolve logger at runtime to ensure authentication failures are always logged
                        ILoggerFactory? loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
                        ILogger requestLogger = loggerFactory?.CreateLogger(typeof(JwtExtensions).FullName ?? "JwtExtensions")
                            ?? logger
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

                        // Extract detailed information for troubleshooting
                        string? tokenAudience = null;
                        string? tokenIssuer = null;
                        string? tokenExpiration = null;
                        string? tokenSubject = null;

                        try
                        {
                            // Try to parse the token to extract claims
                            string authHeader = context.Request.Headers.Authorization.ToString();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                string tokenString = authHeader.Substring("Bearer ".Length).Trim();
                                JwtSecurityToken token = new(tokenString);

                                tokenAudience = token.Audiences?.FirstOrDefault();
                                tokenIssuer = token.Issuer;
                                tokenExpiration = token.ValidTo.ToString("o");
                                tokenSubject = token.Subject;
                            }
                        }
#pragma warning disable CA1031 // Do not catch general exception types - we want to continue logging even if token parsing fails
                        catch
                        {
                            // If we can't parse the token, continue with logging the exception
                        }
#pragma warning restore CA1031

                        // Get configured validation parameters
                        TokenValidationParameters? validationParams = context.Options?.TokenValidationParameters;
                        string configuredAudience = validationParams?.ValidAudience ?? "null";
                        string configuredAudiences = validationParams?.ValidAudiences != null
                            ? string.Join(", ", validationParams.ValidAudiences)
                            : "null";
                        string configuredIssuers = validationParams?.ValidIssuers != null
                            ? string.Join(", ", validationParams.ValidIssuers)
                            : "null";

                        // Log detailed failure information
                        requestLogger.LogError(context.Exception,
                            "JWT Authentication failed for scheme: {Scheme}\n" +
                            "  Failure Reason: {ExceptionMessage}\n" +
                            "  Token Audience: {TokenAudience}\n" +
                            "  Expected Audience: {ConfiguredAudience}\n" +
                            "  Expected Audiences: {ConfiguredAudiences}\n" +
                            "  Token Issuer: {TokenIssuer}\n" +
                            "  Valid Issuers: {ConfiguredIssuers}\n" +
                            "  Token Expiration: {TokenExpiration}\n" +
                            "  Token Subject: {TokenSubject}",
                            schemeName,
                            context.Exception.Message,
                            tokenAudience ?? "Unable to parse",
                            configuredAudience,
                            configuredAudiences,
                            tokenIssuer ?? "Unable to parse",
                            configuredIssuers,
                            tokenExpiration ?? "Unable to parse",
                            tokenSubject ?? "Unable to parse");

                        return Task.CompletedTask;
                    }
                };
                jwtOptions.Validate();
            });
            return builder;
        }
    }
}
