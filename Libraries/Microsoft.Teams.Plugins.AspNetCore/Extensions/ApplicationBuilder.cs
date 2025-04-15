using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApp UseTeams(this IApplicationBuilder builder, bool routing = true)
    {
        var app = builder.ApplicationServices.GetService<IApp>() ?? new App(builder.ApplicationServices.GetService<IAppOptions>());
        var plugins = builder.ApplicationServices.GetServices<IPlugin>();

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
            builder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
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