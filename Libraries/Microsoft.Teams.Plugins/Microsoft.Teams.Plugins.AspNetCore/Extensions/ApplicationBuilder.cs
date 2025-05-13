using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    public static App UseTeams(this IApplicationBuilder builder, bool routing = true)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var app = builder.ApplicationServices.GetService<App>() ?? new App(builder.ApplicationServices.GetService<AppOptions>());
        var plugins = builder.ApplicationServices.GetServices<IPlugin>();
        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<TeamsControllerAttribute>();

            if (attribute is null)
            {
                continue;
            }

            var controller = builder.ApplicationServices.GetService(type);

            if (controller is not null)
            {
                app.AddController(controller);
            }
        }

        foreach (var plugin in plugins)
        {
            app.AddPlugin(plugin);

            if (plugin is IAspNetCorePlugin aspNetCorePlugin)
            {
                aspNetCorePlugin.Configure(builder);
            }
        }

        if (routing)
        {
            builder.UseRouting();
            builder.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        return app;
    }

    public static AspNetCorePlugin GetAspNetCorePlugin(this IApplicationBuilder builder)
    {
        return builder.ApplicationServices.GetAspNetCorePlugin();
    }

    public static AspNetCorePlugin GetAspNetCorePlugin(this IServiceProvider provider)
    {
        return provider.GetRequiredService<AspNetCorePlugin>();
    }
}