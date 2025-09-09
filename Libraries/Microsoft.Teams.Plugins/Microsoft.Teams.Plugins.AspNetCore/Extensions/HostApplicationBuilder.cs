﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
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
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, bool routing = true)
    {
        builder.AddTeamsCore();
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication();

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
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, App app, bool routing = true)
    {
        builder.AddTeamsCore(app);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication();

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
    /// <param name="builder">your app options</param>
    /// <param name="routing">set to false to disable the plugins default http controller</param>
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, AppOptions options, bool routing = true)
    {
        builder.AddTeamsCore(options);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication();

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
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, AppBuilder appBuilder, bool routing = true)
    {
        builder.AddTeamsCore(appBuilder);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.AddTeamsTokenAuthentication();

        if (routing)
        {
            builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        }

        return builder;
    }

    private static IHostApplicationBuilder AddTeamsTokenAuthentication(this IHostApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetTeams();
        Console.WriteLine("Configuring JWT Bearer Authentication");

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
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

            options.TokenValidationParameters.EnableAadSigningKeyIssuerValidation();
            options.Events = new()
            {
                OnMessageReceived = async context =>
                {
                    var header = context.Request.Headers.Authorization.ToString();

                    if (string.IsNullOrEmpty(header))
                    {
                        context.Options.TokenValidationParameters.ConfigurationManager ??= options.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    var parts = header.Split(' ');

                    if (parts.Length != 2 || parts.First() != "Bearer")
                    {
                        context.Options.TokenValidationParameters.ConfigurationManager ??= options.ConfigurationManager as BaseConfigurationManager;
                        await Task.CompletedTask.ConfigureAwait(false);
                        return;
                    }

                    var token = new JwtSecurityToken(parts[1]);
                    var issuer = token.Claims.FirstOrDefault(claim => claim.Type == "iss")?.Value;

                    if (issuer == "https://api.botframework.com")
                    {
                        context.Options.TokenValidationParameters.ConfigurationManager = _openIdMetadataCache.GetOrAdd(settings.Activity.OpenIdMetadataUrl, key =>
                        {
                            return new ConfigurationManager<OpenIdConnectConfiguration>(settings.Activity.OpenIdMetadataUrl, new OpenIdConnectConfigurationRetriever(), new HttpClient())
                            {
                                AutomaticRefreshInterval = BaseConfigurationManager.DefaultAutomaticRefreshInterval
                            };
                        });
                    }

                    await Task.CompletedTask.ConfigureAwait(false);
                },
            };
        });

        return builder;
    }
}