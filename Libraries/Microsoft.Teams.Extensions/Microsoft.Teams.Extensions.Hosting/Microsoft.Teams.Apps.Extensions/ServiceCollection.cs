// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeams(this IServiceCollection collection)
    {
        collection.AddSingleton<Common.Storage.LocalStorage<object>>();
        collection.AddSingleton<Common.Storage.IStorage<string, object>>(provider => provider.GetRequiredService<Common.Storage.LocalStorage<object>>());
        collection.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<TeamsSettings>();
            var logger = provider.GetRequiredService<ILogger<App>>();
            return App.Builder(settings.Apply()).Build(logger);
        });

        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);

        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, AppOptions options)
    {
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<App>>();
            return new App(logger, options);
        });

        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, AppBuilder builder)
    {
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<App>>();
            return builder.Build(logger);
        });

        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, App app)
    {
        collection.AddSingleton(app);
        collection.AddSingleton(app.Storage);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, Func<IServiceProvider, App> factory)
    {
        collection.AddSingleton(factory);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, Func<IServiceProvider, Task<App>> factory)
    {
        collection.AddSingleton(provider => factory(provider).GetAwaiter().GetResult());
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeamsPlugin<TPlugin>(this IServiceCollection collection) where TPlugin : class, IPlugin
    {
        collection.AddSingleton<TPlugin>();
        collection.AddSingleton<IPlugin, TPlugin>(provider => provider.GetRequiredService<TPlugin>());
        return collection.AddHostedService<TeamsPluginService<TPlugin>>();
    }

    public static IServiceCollection AddTeamsPlugin<TPlugin>(this IServiceCollection collection, TPlugin plugin) where TPlugin : class, IPlugin
    {
        collection.AddSingleton(plugin);
        collection.AddSingleton<IPlugin>(provider => provider.GetRequiredService<TPlugin>());
        return collection.AddHostedService<TeamsPluginService<TPlugin>>();
    }

    public static IServiceCollection AddTeamsPlugin<TPlugin>(this IServiceCollection collection, Func<IServiceProvider, TPlugin> factory) where TPlugin : class, IPlugin
    {
        collection.AddSingleton(factory);
        collection.AddSingleton<IPlugin, TPlugin>(factory);
        return collection.AddHostedService<TeamsPluginService<TPlugin>>();
    }
}