// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Builder;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Plugins.AspNetCore;

namespace Microsoft.Teams.Plugins.External.Mcp;

[Plugin(
    "High-level MCP server that provides a simpler API for working with resources, tools, and prompts.",
    "For advanced usage (like sending notifications or setting custom request handlers),",
    "use the underlying Server instance available via the server property."
)]
public class McpPlugin : IAspNetCorePlugin
{
    private readonly ILogger<McpPlugin> Logger;

    public McpPlugin(ILogger<McpPlugin>? logger = null)
    {
        Logger = logger ?? LoggerFactory.Create(builder => { }).CreateLogger<McpPlugin>();
    }

    public event EventFunction Events;

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        builder.UseRouting();
        return builder.UseEndpoints(endpoints => endpoints.MapMcp("mcp"));
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnStart");
        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnError");
        return Task.CompletedTask;
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnActivity");
        return Task.CompletedTask;
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnActivitySent");
        return Task.CompletedTask;
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnActivityResponse");
        return Task.CompletedTask;
    }
}
