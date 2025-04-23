using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddTeamsDevTools(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetTeamsDevTools());
        builder.Services.AddTeamsPlugin(provider =>
        {
            var plugin = provider.GetRequiredService<DevToolsPlugin>();
            var settings = provider.GetRequiredService<TeamsDevToolsSettings>();

            foreach (var page in settings.Pages)
            {
                plugin.AddPage(page);
            }

            return plugin;
        });

        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }
}