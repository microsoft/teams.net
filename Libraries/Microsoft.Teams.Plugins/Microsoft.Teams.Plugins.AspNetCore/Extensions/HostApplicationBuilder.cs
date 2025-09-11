// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Validators;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class HostApplicationBuilderExtensions
{
    private static readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache = new();

    /// <summary>
    /// adds core Teams services and the
    /// AspNetCorePlugin
    /// </summary>
    /// <param name="routing">set to false to disable the plugins default http controller</param>
    /// <param name="skipAuth">set to true to disable token authentication</param>
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, bool routing = true, bool skipAuth = false)
    {
        builder.AddTeamsCore();
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication(skipAuth);

        if (routing)
        {
            builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        }

        return builder;
    }

    /// <summary>
    /// adds core Teams services and the
    /// AspNetCorePlugin
    /// </summary>
    /// <param name="app">your app instance</param>
    /// <param name="routing">set to false to disable the plugins default http controller</param>
    /// <param name="skipAuth">set to true to disable token authentication</param>
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, App app, bool routing = true, bool skipAuth = false)
    {
        builder.AddTeamsCore(app);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication(skipAuth);


        if (routing)
        {
            builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        }

        return builder;
    }

    /// <summary>
    /// adds core Teams services and the
    /// AspNetCorePlugin
    /// </summary>
    /// <param name="options">your app options</param>
    /// <param name="routing">set to false to disable the plugins default http controller</param>
    /// <param name="skipAuth">set to true to disable token authentication</param>
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, AppOptions options, bool routing = true, bool skipAuth = false)
    {
        builder.AddTeamsCore(options);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication(skipAuth);

        if (routing)
        {
            builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        }

        return builder;
    }

    /// <summary>
    /// adds core Teams services and the
    /// AspNetCorePlugin
    /// </summary>
    /// <param name="appBuilder">your app builder</param>
    /// <param name="routing">set to false to disable the plugins default http controller</param>
    /// <param name="skipAuth">set to true to disable token authentication</param>
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, AppBuilder appBuilder, bool routing = true, bool skipAuth = false)
    {
        builder.AddTeamsCore(appBuilder);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication(skipAuth);

        if (routing)
        {
            builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        }

        return builder;
    }

    /// <summary>
    /// adds authentication and authorization to validate incoming Teams tokens
    /// </summary>
    /// <returns></returns>
    private static IHostApplicationBuilder AddTeamsTokenAuthentication(this IHostApplicationBuilder builder, bool skipAuth = false)
    {
        var settings = builder.Configuration.GetTeams();

        if (string.IsNullOrEmpty(settings.ClientId))
        {
            return builder;
        }

        settings.AddDefaultAudiences();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer("TeamsJWTScheme", options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                ValidIssuers = settings.Activity.Issuers,
                ValidAudiences = settings.Activity.Audiences,
            };

            // stricter validation: ensures the key’s issuer matches the token issuer
            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
            // use cached OpenID Connect metadata
            options.ConfigurationManager = _openIdMetadataCache.GetOrAdd(
            settings.Activity.OpenIdMetadataUrl,
            key => new ConfigurationManager<OpenIdConnectConfiguration>(
                settings.Activity.OpenIdMetadataUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpClient())
            {
                AutomaticRefreshInterval = BaseConfigurationManager.DefaultAutomaticRefreshInterval
            });
        });

        // add [Authorize(Policy="..")] support for endpoints
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("TeamsJWTPolicy", policy =>
            {
                if (skipAuth)
                {
                    // bypass authentication
                    policy.RequireAssertion(_ => true);
                }
                else
                {
                    policy.AddAuthenticationSchemes("TeamsJWTScheme");
                    policy.RequireAuthenticatedUser();
                }
            });
        });

        return builder;
    }
}