// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class HostApplicationBuilderExtensions
{
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

    public static class TeamsTokenAuthConstants
    {
        // the authentication scheme for validating incoming Teams tokens
        public const string AuthenticationScheme = "TeamsJWTScheme";
        // the authorization policy attached to endpoints or controllers
        public const string AuthorizationPolicy = "TeamsJWTPolicy";
    }

    /// <summary>
    /// adds authentication and authorization to validate incoming Teams tokens
    /// </summary>
    /// <returns></returns>
    private static IHostApplicationBuilder AddTeamsTokenAuthentication(this IHostApplicationBuilder builder, bool skipAuth = false)
    {
        var settings = builder.Configuration.GetTeams();

        var teamsValidationSettings = new TeamsValidationSettings();
        if (!string.IsNullOrEmpty(settings.ClientId))
        {
            teamsValidationSettings.AddDefaultAudiences(settings.ClientId);
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(TeamsTokenAuthConstants.AuthenticationScheme, options =>
        {
            TokenValidator.ConfigureValidation(options, teamsValidationSettings.Issuers, teamsValidationSettings.Audiences, teamsValidationSettings.OpenIdMetadataUrl);
        });

        // add [Authorize(Policy="..")] support for endpoints
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(TeamsTokenAuthConstants.AuthorizationPolicy, policy =>
            {
                if (skipAuth || string.IsNullOrEmpty(settings.ClientId))
                {
                    // bypass authentication
                    policy.RequireAssertion(_ => true);
                }
                else
                {
                    policy.AddAuthenticationSchemes(TeamsTokenAuthConstants.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                }
            });
        });

        return builder;
    }
}