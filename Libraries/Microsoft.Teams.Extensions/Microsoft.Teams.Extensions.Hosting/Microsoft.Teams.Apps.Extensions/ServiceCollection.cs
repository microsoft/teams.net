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

        collection.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        collection.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<TeamsSettings>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return App.Builder(settings.Apply()).AddLoggerFactory(loggerFactory).Build();
        });

        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Logger);

        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, AppOptions options)
    {
        collection.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        collection.AddSingleton(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            options.LoggerFactory = loggerFactory;
            return new App(options);
        });

        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Logger);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, AppBuilder builder)
    {
        collection.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        collection.AddSingleton(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return builder.AddLoggerFactory(loggerFactory).Build();
        });

        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Logger);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, App app)
    {
        collection.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        collection.AddSingleton(app);
        collection.AddSingleton(app.Storage);
        collection.AddSingleton(app.Logger);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, Func<IServiceProvider, App> factory)
    {
        collection.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        collection.AddSingleton(factory);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Logger);
        collection.AddHostedService<TeamsService>();
        collection.AddSingleton<IContext.Accessor>();
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, Func<IServiceProvider, Task<App>> factory)
    {
        collection.AddLogging(builder =>
        {
            builder.AddConsole();
        });
        collection.AddSingleton(provider => factory(provider).GetAwaiter().GetResult());
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Storage);
        collection.AddSingleton(provider => provider.GetRequiredService<App>().Logger);
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