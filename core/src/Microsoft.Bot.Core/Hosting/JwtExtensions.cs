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

namespace Microsoft.Bot.Core.Hosting
{
    /// <summary>
    /// Provides extension methods for configuring JWT authentication and authorization for bots and agents.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
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
                builder.AddCustomJwtBearer(BotScheme, validIssuers, audience, logger);
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
        /// <param name="logger">The logger instance for logging.</param>
        /// <returns>An <see cref="AuthorizationBuilder"/> for further authorization configuration.</returns>
        public static AuthorizationBuilder AddAuthorization(this IServiceCollection services, ILogger logger, string aadSectionName = "AzureAd")
        {
            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            string azureScope = configuration[$"Scope"]!;
            bool useAgentAuth = false;

            if (string.Equals(azureScope, AgentScope, StringComparison.OrdinalIgnoreCase))
            {
                useAgentAuth = true;
            }

            services.AddBotAuthentication(configuration, useAgentAuth, logger, aadSectionName);
            AuthorizationBuilder authorizationBuilder = services
                .AddAuthorizationBuilder()
                .AddDefaultPolicy("DefaultPolicy", policy =>
                {
                    if (!useAgentAuth)
                    {
                        policy.AuthenticationSchemes.Add(BotScheme);
                    }
                    else
                    {
                        policy.AuthenticationSchemes.Add(AgentScheme);
                    }
                    policy.RequireAuthenticatedUser();
                });
            return authorizationBuilder;
        }

        private static AuthenticationBuilder AddCustomJwtBearer(this AuthenticationBuilder builder, string schemeName, string[] validIssuers, string audience, ILogger logger)
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
                        logger.LogDebug("OnMessageReceived invoked for scheme: {Scheme}", schemeName);
                        string authorizationHeader = context.Request.Headers.Authorization.ToString();

                        if (string.IsNullOrEmpty(authorizationHeader))
                        {
                            // Default to AadTokenValidation handling
                            context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                            await Task.CompletedTask.ConfigureAwait(false);
                            logger.LogWarning("Authorization header is missing.");
                            return;
                        }

                        string[] parts = authorizationHeader?.Split(' ')!;
                        if (parts.Length != 2 || parts[0] != "Bearer")
                        {
                            // Default to AadTokenValidation handling
                            context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                            await Task.CompletedTask.ConfigureAwait(false);
                            logger.LogWarning("Invalid authorization header format.");
                            return;
                        }

                        JwtSecurityToken token = new(parts[1]);
                        string issuer = token.Claims.FirstOrDefault(claim => claim.Type == "iss")?.Value!;
                        string tid = token.Claims.FirstOrDefault(claim => claim.Type == "tid")?.Value!;

                        string oidcAuthority = issuer.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                            ? BotOIDC : $"{AgentOIDC}{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";

                        logger.LogDebug("Using OIDC Authority: {OidcAuthority} for issuer: {Issuer}", oidcAuthority, issuer);

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
                        logger.LogInformation("Token validated successfully for scheme: {Scheme}", schemeName);
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        logger.LogWarning("Forbidden response for scheme: {Scheme}", schemeName);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        logger.LogWarning("Authentication failed for scheme: {Scheme}. Exception: {Exception}", schemeName, context.Exception);
                        return Task.CompletedTask;
                    }
                };
                jwtOptions.Validate();
            });
            return builder;
        }
    }
}
