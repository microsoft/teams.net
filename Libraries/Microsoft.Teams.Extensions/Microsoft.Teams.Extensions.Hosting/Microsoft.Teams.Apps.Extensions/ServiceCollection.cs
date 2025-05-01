using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Extensions.Logging;

namespace Microsoft.Teams.Apps.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeams(this IServiceCollection collection)
    {
        collection.AddSingleton<Common.Logging.ConsoleLogger>();
        collection.AddSingleton<Common.Logging.ILogger, Common.Logging.ConsoleLogger>(provider => provider.GetRequiredService<Common.Logging.ConsoleLogger>());
        collection.AddSingleton<Common.Storage.LocalStorage<object>>();
        collection.AddSingleton<Common.Storage.IStorage<string, object>>(provider => provider.GetRequiredService<Common.Storage.LocalStorage<object>>());

        collection.AddSingleton<TeamsLogger>();
        collection.AddSingleton<ILogger, TeamsLogger>(provider => provider.GetRequiredService<TeamsLogger>());
        collection.AddSingleton<ILoggerFactory, LoggerFactory>(provider =>
        {
            var logger = provider.GetRequiredService<TeamsLogger>();
            return new LoggerFactory([new TeamsLoggerProvider(logger)]);
        });

        collection.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<TeamsSettings>();
            var logger = provider.GetRequiredService<Common.Logging.ILogger>();
            return App.Builder(settings.Apply()).AddLogger(logger).Build();
        });

        collection.AddHostedService<TeamsService>();
        collection.AddScoped<TeamsContext>();
        collection.AddTransient(provider => provider.GetRequiredService<TeamsContext>().Activity);
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, IAppOptions options)
    {
        var app = new App(options);
        var log = new TeamsLogger(app.Logger);

        collection.AddSingleton(app.Logger);
        collection.AddSingleton(app.Storage);
        collection.AddSingleton<ILoggerFactory>(_ => new LoggerFactory([new TeamsLoggerProvider(log)]));
        collection.AddSingleton<ILogger>(log);
        collection.AddSingleton<IApp>(app);
        collection.AddHostedService<TeamsService>();
        collection.AddScoped<TeamsContext>();
        collection.AddTransient(provider => provider.GetRequiredService<TeamsContext>().Activity);
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, IAppBuilder builder)
    {
        var app = builder.Build();
        var log = new TeamsLogger(app.Logger);

        collection.AddSingleton(app.Logger);
        collection.AddSingleton(app.Storage);
        collection.AddSingleton<ILoggerFactory, LoggerFactory>(_ => new LoggerFactory([new TeamsLoggerProvider(log)]));
        collection.AddSingleton<ILogger>(log);
        collection.AddSingleton(app);
        collection.AddHostedService<TeamsService>();
        collection.AddScoped<TeamsContext>();
        collection.AddTransient(provider => provider.GetRequiredService<TeamsContext>().Activity);
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, IApp app)
    {
        var log = new TeamsLogger(app.Logger);

        collection.AddSingleton(app.Logger);
        collection.AddSingleton(app.Storage);
        collection.AddSingleton<ILoggerFactory, LoggerFactory>(_ => new LoggerFactory([new TeamsLoggerProvider(log)]));
        collection.AddSingleton<ILogger>(log);
        collection.AddSingleton(app);
        collection.AddHostedService<TeamsService>();
        collection.AddScoped<TeamsContext>();
        collection.AddTransient(provider => provider.GetRequiredService<TeamsContext>().Activity);
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, Func<IServiceProvider, IApp> factory)
    {
        collection.AddSingleton(provider => provider.GetRequiredService<Common.Logging.ILogger>());
        collection.AddSingleton<ILoggerFactory, LoggerFactory>();
        collection.AddSingleton<ILogger, TeamsLogger>(provider => provider.GetRequiredService<TeamsLogger>());
        collection.AddHostedService<TeamsService>();
        collection.AddScoped<TeamsContext>();
        collection.AddTransient(provider => provider.GetRequiredService<TeamsContext>().Activity);
        collection.AddSingleton(factory);
        collection.AddSingleton(provider => provider.GetRequiredService<IApp>().Logger);
        collection.AddSingleton(provider => provider.GetRequiredService<IApp>().Storage);
        return collection;
    }

    public static IServiceCollection AddTeams(this IServiceCollection collection, Func<IServiceProvider, Task<IApp>> factory)
    {
        collection.AddSingleton(provider => provider.GetRequiredService<Common.Logging.ILogger>());
        collection.AddSingleton<ILoggerFactory, LoggerFactory>();
        collection.AddSingleton<ILogger, TeamsLogger>(provider => provider.GetRequiredService<TeamsLogger>());
        collection.AddHostedService<TeamsService>();
        collection.AddScoped<TeamsContext>();
        collection.AddTransient(provider => provider.GetRequiredService<TeamsContext>().Activity);
        collection.AddSingleton(provider => factory(provider).GetAwaiter().GetResult());
        collection.AddSingleton(provider => provider.GetRequiredService<IApp>().Logger);
        collection.AddSingleton(provider => provider.GetRequiredService<IApp>().Storage);
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