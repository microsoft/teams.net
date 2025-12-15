// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;

namespace Microsoft.Bot.Core.Hosting
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
        /// <returns>An <see cref="AuthenticationBuilder"/> for further authentication configuration.</returns>
        public static AuthenticationBuilder AddBotAuthentication(this IServiceCollection services, IConfiguration configuration, bool useAgentAuth, string aadSectionName = "AzureAd")
        {
            AuthenticationBuilder builder = services.AddAuthentication();
            ArgumentNullException.ThrowIfNull(configuration);
            string audience = configuration[$"{aadSectionName}:ClientId"]!;

            if (!useAgentAuth)
            {
                string[] validIssuers = ["https://api.botframework.com"];
                builder.AddCustomJwtBearer(BotScheme, validIssuers, audience);
            } else
            {
                string tenantId = configuration[$"{aadSectionName}:TenantId"]!;
                string[] validIssuers = [$"https://sts.windows.net/{tenantId}/", $"https://login.microsoftonline.com/{tenantId}/v2", "https://api.botframework.com"];
                builder.AddCustomJwtBearer(AgentScheme, validIssuers, audience);
            }
            return builder;
        }

        /// <summary>
        /// Adds authorization policies to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add authorization to.</param>
        /// <param name="aadSectionName">The configuration section name for the settings. Defaults to "AzureAd".</param>
        /// <returns>An <see cref="AuthorizationBuilder"/> for further authorization configuration.</returns>
        public static AuthorizationBuilder AddAuthorization(this IServiceCollection services, string aadSectionName = "AzureAd")
        {
            IConfiguration configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            string azureScope = configuration[$"{aadSectionName}:Scope"]!;
            bool useAgentAuth = false;

            if (string.Equals(azureScope, AgentScope, StringComparison.OrdinalIgnoreCase))
            {
                useAgentAuth = true;
            }

            services.AddBotAuthentication(configuration, useAgentAuth, aadSectionName);
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

        /// <summary>
        /// Adds a custom JWT Bearer authentication scheme with specified valid issuers and audience.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/> to add the JWT Bearer scheme to.</param>
        /// <param name="schemeName">The name of the authentication scheme.</param>
        /// <param name="validIssuers">An array of valid issuer strings for token validation.</param>
        /// <param name="audience">The expected audience for the JWT token.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/> for further configuration.</returns>
        public static AuthenticationBuilder AddCustomJwtBearer(this AuthenticationBuilder builder, string schemeName, string[] validIssuers, string audience)
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
                        string authorizationHeader = context.Request.Headers.Authorization.ToString();

                        if (string.IsNullOrEmpty(authorizationHeader))
                        {
                            // Default to AadTokenValidation handling
                            context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                            await Task.CompletedTask.ConfigureAwait(false);
                            return;
                        }

                        string[] parts = authorizationHeader?.Split(' ')!;
                        if (parts.Length != 2 || parts[0] != "Bearer")
                        {
                            // Default to AadTokenValidation handling
                            context.Options.TokenValidationParameters.ConfigurationManager ??= jwtOptions.ConfigurationManager as BaseConfigurationManager;
                            await Task.CompletedTask.ConfigureAwait(false);
                            return;
                        }

                        JwtSecurityToken token = new(parts[1]);
                        string issuer = token.Claims.FirstOrDefault(claim => claim.Type == "iss")?.Value!;
                        string tid = token.Claims.FirstOrDefault(claim => claim.Type == "tid")?.Value!;

                        string oidcAuthority = issuer.Equals("https://api.botframework.com", StringComparison.OrdinalIgnoreCase)
                            ? BotOIDC : $"{AgentOIDC}{tid ?? "botframework.com"}/v2.0/.well-known/openid-configuration";

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
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    }
                };
                jwtOptions.Validate();
            });
            return builder;
        }
    }
}
