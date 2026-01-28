// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Plugins;

using static Microsoft.Teams.Plugins.AspNetCore.Extensions.HostApplicationBuilderExtensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static partial class ApplicationBuilderExtensions
{
    /// <summary>
    /// initializes/starts your Teams app after
    /// adding all registered IPlugin's
    /// </summary>
    /// <param name="routing">set to false to disable the plugins default http controller endpoints</param>
    /// <returns>your app instance</returns>
    public static App UseTeams(this IApplicationBuilder builder, bool routing = true)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var app = builder.ApplicationServices.GetService<App>() ?? new App(builder.ApplicationServices.GetService<AppOptions>());
        var plugins = builder.ApplicationServices.GetServices<IPlugin>();
        var types = assembly.GetTypes();

        foreach (var type in types)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var attribute = type.GetCustomAttribute<TeamsControllerAttribute>();
#pragma warning restore CS0618 // Type or member is obsolete

            if (attribute is null)
            {
                continue;
            }

            var controller = builder.ApplicationServices.GetService(type);

            if (controller is not null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                app.AddController(controller);
#pragma warning restore CS0618 // Type or member is obsolete
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
            builder.UseAuthorization();
            
            // Get AspNetCorePlugin for endpoint registration
            var aspNetCorePlugin = plugins.OfType<AspNetCorePlugin>().FirstOrDefault();
            
            builder.UseEndpoints(endpoints =>
            {
                // Map AspNetCorePlugin endpoint
                if (aspNetCorePlugin is not null)
                {
                    endpoints.MapPost("/api/messages", async (HttpContext httpContext, CancellationToken cancellationToken) =>
                    {
                        return await aspNetCorePlugin.Do(httpContext, cancellationToken);
                    }).RequireAuthorization(TeamsTokenAuthConstants.AuthorizationPolicy);
                }

                // Map controller endpoints (obsolete)
                endpoints.MapControllers();
            });
        }

        return app;
    }

    /// <summary>
    /// get the AspNetCorePlugin instance
    /// </summary>
    public static AspNetCorePlugin GetAspNetCorePlugin(this IApplicationBuilder builder)
    {
        return builder.ApplicationServices.GetAspNetCorePlugin();
    }
}