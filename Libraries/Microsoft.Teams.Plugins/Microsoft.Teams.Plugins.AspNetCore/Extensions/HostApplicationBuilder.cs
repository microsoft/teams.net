// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    public static class EntraTokenAuthConstants
    {
        public const string AuthenticationScheme = "EntraTokenJWTScheme";
        public const string AuthorizationPolicy = "EntraTokenJWTPolicy";
    }

    /// <summary>
    /// add TeamsJWTScheme for validating incoming SMBA tokens and EntraTokenJWTScheme for validating incoming Entra tokens
    /// provides Authorization policy TeamsJWTPolicy required by [Authorize(Policy="TeamsJWTPolicy")] in MessageController
    /// provides Authorization policy EntraTokenJWTPolicy required when Tab invokes remote functions
    /// </summary>
    /// <returns></returns>
    public static IHostApplicationBuilder AddTeamsTokenAuthentication(this IHostApplicationBuilder builder, bool skipAuth = false)
    {
        var settings = builder.Configuration.GetTeams();
        var cloud = settings.ResolveCloud();

        var teamsValidationSettings = new TeamsValidationSettings(cloud);
        if (!string.IsNullOrEmpty(settings.ClientId))
        {
            teamsValidationSettings.AddDefaultAudiences(settings.ClientId);
        }
        else if (skipAuth)
        {
            // No Teams:ClientId configured and skipAuth is set, so the authorization
            // policy bypasses authentication and the bot will accept anonymous traffic.
            // The warning routes through whatever logging pipeline the consumer set up.
            LogFromServices(builder.Services, l => l.LogWarning(
                "No Teams:ClientId configured and skipAuth is enabled. Bot will accept unauthenticated requests on the messaging endpoint."));
        }
        else
        {
            // No Teams:ClientId configured and skipAuth is not set, so the authorization
            // policy rejects every request to the messaging endpoint. Warn the consumer
            // their bot will not receive traffic until credentials are configured (or
            // skipAuth: true is passed to AddTeams(...) for local development).
            LogFromServices(builder.Services, l => l.LogWarning(
                "No Teams:ClientId configured. Bot will reject all requests on the messaging endpoint until credentials are configured."));
        }

        builder.Services.
            AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(TeamsTokenAuthConstants.AuthenticationScheme, options =>
            {
                TokenValidator.ConfigureValidation(options, teamsValidationSettings.Issuers, teamsValidationSettings.Audiences, teamsValidationSettings.OpenIdMetadataUrl);
            })
            .AddJwtBearer(EntraTokenAuthConstants.AuthenticationScheme, options =>
            {
                TokenValidator.ConfigureValidation(options, teamsValidationSettings.GetValidIssuersForTenant(settings.TenantId), teamsValidationSettings.Audiences, teamsValidationSettings.GetTenantSpecificOpenIdMetadataUrl(settings.TenantId));
            });


        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(TeamsTokenAuthConstants.AuthorizationPolicy, policy =>
            {
                if (skipAuth)
                {
                    // bypass authentication
                    policy.RequireAssertion(_ => true);
                }
                else if (string.IsNullOrEmpty(settings.ClientId))
                {
                    // No credentials configured: reject all requests. Pass
                    // skipAuth: true to AddTeams(...) to opt into the bypass
                    // for local development without credentials.
                    policy.RequireAssertion(_ => false);
                }
                else
                {
                    policy.AddAuthenticationSchemes(TeamsTokenAuthConstants.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                }
            });

            // token validation policy for Entra tokens, used when tab apps invoke remote functions
            options.AddPolicy(EntraTokenAuthConstants.AuthorizationPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(EntraTokenAuthConstants.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        });

        return builder;
    }

    /// <summary>
    /// Invoke <paramref name="action"/> with an <see cref="ILogger"/> resolved from the
    /// service collection during DI configuration. Prefers an already-registered
    /// <see cref="ILoggerFactory"/> instance; otherwise builds a temporary
    /// <see cref="ServiceProvider"/>, invokes <paramref name="action"/> inside its
    /// <c>using</c> scope, and disposes cleanly. Passes <see cref="NullLogger.Instance"/>
    /// when no <see cref="ILoggerFactory"/> is registered.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>LogFromServices</c> in <c>core/src/Microsoft.Teams.Core/Hosting/AddBotApplicationExtensions.cs</c>.
    /// </remarks>
    internal static void LogFromServices(IServiceCollection services, Action<ILogger> action, Type? categoryType = null)
    {
        ServiceDescriptor? descriptor = services.LastOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (descriptor is null)
        {
            action(NullLogger.Instance);
            return;
        }

        if (descriptor.ImplementationInstance is ILoggerFactory directFactory)
        {
            action(directFactory.CreateLogger(categoryType ?? typeof(HostApplicationBuilderExtensions)));
            return;
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        ILoggerFactory? factory = tempProvider.GetService<ILoggerFactory>();
        action(factory?.CreateLogger(categoryType ?? typeof(HostApplicationBuilderExtensions)) ?? NullLogger.Instance);
    }
}