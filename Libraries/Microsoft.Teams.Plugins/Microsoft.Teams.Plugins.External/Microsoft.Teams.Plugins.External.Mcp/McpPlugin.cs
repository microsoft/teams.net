using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Builder;
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
    [AllowNull]
    [Dependency]
    public ILogger Logger { get; set; }
    
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