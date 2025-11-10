// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddTeamsCore(this IHostApplicationBuilder builder)
    {
        return AddTeamsCore(builder, new AppOptions());
    }

    public static IHostApplicationBuilder AddTeamsCore(this IHostApplicationBuilder builder, App app)
    {
        builder.Services.AddSingleton(builder.Configuration.GetTeams());
        builder.Services.AddTeams(app);
        return builder;
    }

    public static IHostApplicationBuilder AddTeamsCore(this IHostApplicationBuilder builder, AppOptions options)
    {
        var settings = builder.Configuration.GetTeams();
        // client credentials
        if (options.Credentials is null && settings.ClientId is not null && settings.ClientSecret is not null && !settings.Empty)
        {
            options.Credentials = new ClientCredentials(
                settings.ClientId,
                settings.ClientSecret,
                settings.TenantId
            );
        }

        var app = new App(options);

        builder.Services.AddSingleton(settings);
        builder.Services.AddTeams(app);
        return builder;
    }

    public static IHostApplicationBuilder AddTeamsCore(this IHostApplicationBuilder builder, AppBuilder appBuilder)
    {
        var settings = builder.Configuration.GetTeams();

        // client credentials
        if (settings.ClientId is not null && settings.ClientSecret is not null && !settings.Empty)
        {
            appBuilder = appBuilder.AddCredentials(new ClientCredentials(
                settings.ClientId,
                settings.ClientSecret,
                settings.TenantId
            ));
        }

        var app = appBuilder.Build();

        builder.Services.AddSingleton(settings);
        builder.Services.AddTeams(app);
        return builder;
    }

    public static IHostApplicationBuilder AddTeamsPlugin<TPlugin>(this IHostApplicationBuilder builder) where TPlugin : class, IPlugin
    {
        builder.Services.AddTeamsPlugin<TPlugin>();
        return builder;
    }

    public static IHostApplicationBuilder AddTeamsPlugin<TPlugin>(this IHostApplicationBuilder builder, TPlugin plugin) where TPlugin : class, IPlugin
    {
        builder.Services.AddTeamsPlugin(plugin);
        return builder;
    }
}