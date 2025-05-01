using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Builder;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
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

    public event IPlugin.ErrorEventHandler ErrorEvent = (_, _) => Task.Run(() => { });

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        builder.UseRouting();
        return builder.UseEndpoints(endpoints => endpoints.MapMcp("mcp"));
    }

    public Task OnInit(IApp app, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => { });
    }

    public Task OnStart(IApp app, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnStart"));
    }

    public Task OnError(IApp app, IPlugin? plugin, Exception exception, IContext<IActivity>? context, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnError"));
    }

    public Task OnActivity(IApp app, IContext<IActivity> context)
    {
        return Task.Run(() => Logger.Debug("OnActivity"));
    }

    public Task OnActivityResponse(IApp app, Response? response, IContext<IActivity> context)
    {
        return Task.Run(() => Logger.Debug("OnActivityResponse"));
    }

    public Task OnActivitySent(IApp app, IActivity activity, IContext<IActivity> context)
    {
        return Task.Run(() => Logger.Debug("OnActivitySent"));
    }

    public Task OnActivitySent(IApp app, ISenderPlugin sender, IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnActivitySent"));
    }
}