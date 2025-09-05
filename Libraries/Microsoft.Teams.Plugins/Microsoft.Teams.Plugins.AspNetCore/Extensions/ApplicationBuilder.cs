// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class ApplicationBuilderExtensions
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

    /// <summary>
    /// get the AspNetCorePlugin instance
    /// </summary>
    public static AspNetCorePlugin GetAspNetCorePlugin(this IApplicationBuilder builder)
    {
        return builder.ApplicationServices.GetAspNetCorePlugin();
    }

    /// <summary>
    /// add/update a static tab.
    /// the tab will be hosted at
    /// <code>http://localhost:{{PORT}}/tabs/{{name}}</code> or
    /// <code>https://{{BOT_DOMAIN}}/tabs/{{name}}</code>
    /// </summary>
    /// <param name="name">A unique identifier for the entity which the tab displays</param>
    /// <param name="provider">The file provider used to serve static assets</param>
    public static IApplicationBuilder AddTeamsTab(this IApplicationBuilder builder, string name, IFileProvider provider)
    {
        IResult OnGet(string path)
        {
            var file = provider.GetFileInfo(path);

            if (!file.Exists)
            {
                return Results.NotFound($"file \"{path}\" not found");
            }

            return Results.File(file.CreateReadStream(), contentType: "text/html");
        }

        builder.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = provider,
            ServeUnknownFileTypes = true,
            RequestPath = $"/tabs/{name}"
        });

        builder.UseEndpoints(endpoints =>
        {
            endpoints.MapGet($"/tabs/{name}", async context =>
            {
                await OnGet("index.html").ExecuteAsync(context);
            });

            endpoints.MapGet($"/tabs/{name}/{{*path}}", async context =>
            {
                var path = context.GetRouteData().Values["path"]?.ToString();

                if (path is null)
                {
                    await Results.NotFound().ExecuteAsync(context);
                    return;
                }

                await OnGet(path).ExecuteAsync(context);
            });
        });

        return builder;
    }

    /// <summary>
    /// add/update a static tab.
    /// the tab will be hosted at
    /// <code>http://localhost:{{PORT}}/tabs/{{name}}</code> or
    /// <code>https://{{BOT_DOMAIN}}/tabs/{{name}}</code>
    /// </summary>
    /// <param name="name">A unique identifier for the entity which the tab displays</param>
    /// <param name="path">The filepath to use when creating a file provider</param>
    /// <remarks>
    /// The default file provider type is <code>ManifestEmbeddedFileProvider</code>,
    /// to use your own file provider use see <see cref="AddTeamsTab" />
    /// </remarks>
    public static IApplicationBuilder AddTeamsTab(this IApplicationBuilder builder, string name, string path)
    {
        return builder.AddTeamsTab(name, new ManifestEmbeddedFileProvider(Assembly.GetCallingAssembly(), path));
    }
}