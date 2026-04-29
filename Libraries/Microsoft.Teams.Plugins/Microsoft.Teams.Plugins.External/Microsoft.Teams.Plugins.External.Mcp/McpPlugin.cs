// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Plugins.AspNetCore;

namespace Microsoft.Teams.Plugins.External.Mcp;

[Plugin(
    "High-level MCP server that provides a simpler API for working with resources, tools, and prompts.",
    "For advanced usage (like sending notifications or setting custom request handlers),",
    "use the underlying Server instance available via the server property."
)]
public class McpPlugin : IAspNetCorePlugin
{
    internal const string McpPath = "/mcp";

    [AllowNull]
    [Dependency]
    public ILogger Logger { get; set; }

    public event EventFunction Events;

    private readonly McpPluginOptions _options;

    public McpPlugin() : this(new McpPluginOptions()) { }

    public McpPlugin(McpPluginOptions options)
    {
        _options = options;
    }

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        builder.UseRouting();

        if (_options.RequireAuth is not null)
        {
            Func<HttpContext, Task<bool>> requireAuth = _options.RequireAuth;
            builder.Use(async (ctx, next) =>
            {
                if (!ctx.Request.Path.StartsWithSegments(McpPath))
                {
                    await next();
                    return;
                }

                bool ok = false;
                try
                {
                    ok = await requireAuth(ctx);
                }
                catch (Exception ex)
                {
                    Logger.Debug($"RequireAuth threw: {ex}");
                }

                if (!ok)
                {
                    ctx.Response.Headers["WWW-Authenticate"] = "Bearer";
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.WriteAsync("unauthorized");
                    return;
                }

                await next();
            });
        }

        return builder.UseEndpoints(endpoints => endpoints.MapMcp(McpPath.TrimStart('/')));
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        if (_options.RequireAuth is null)
        {
            Logger.Warn(
                $"McpPlugin started without RequireAuth. All MCP requests at {McpPath} will be accepted. " +
                "Pass RequireAuth via AddTeamsMcp(options => options.RequireAuth = ...) to enforce authentication."
            );
        }
        Logger.Debug("OnStart");
        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnError");
        return Task.CompletedTask;
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivity");
        return Task.CompletedTask;
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivitySent");
        return Task.CompletedTask;
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivityResponse");
        return Task.CompletedTask;
    }
}