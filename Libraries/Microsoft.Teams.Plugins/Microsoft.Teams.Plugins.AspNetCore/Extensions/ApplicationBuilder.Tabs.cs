// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static partial class ApplicationBuilderExtensions
{
    /// <summary>
    /// add/update a static tab.
    /// the tab will be hosted at
    /// <code>http://localhost:{{PORT}}/tabs/{{name}}</code> or
    /// <code>https://{{BOT_DOMAIN}}/tabs/{{name}}</code>
    /// </summary>
    /// <param name="name">A unique identifier for the entity which the tab displays</param>
    /// <param name="provider">The file provider used to serve static assets</param>
    public static IApplicationBuilder AddTab(this IApplicationBuilder builder, string name, IFileProvider provider)
    {
        var contentTypeProvider = new FileExtensionContentTypeProvider();

        IResult OnGet(string path)
        {
            var file = provider.GetFileInfo(path);

            if (!file.Exists)
            {
                return Results.NotFound($"file \"{path}\" not found");
            }

            if (!contentTypeProvider.TryGetContentType(file.Name, out string? contentType))
            {
                contentType = "text/html";
            }

            return Results.File(file.CreateReadStream(), contentType);
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
    /// to use your own file provider use see <see cref="AddTab" />
    /// </remarks>
    public static IApplicationBuilder AddTab(this IApplicationBuilder builder, string name, string path)
    {
        return builder.AddTab(name, new ManifestEmbeddedFileProvider(Assembly.GetCallingAssembly(), path));
    }
}