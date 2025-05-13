using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps.Extensions;

using ModelContextProtocol.Server;

namespace Microsoft.Teams.Plugins.External.Mcp.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddTeamsMcp(this IHostApplicationBuilder builder)
    {
        return builder.AddTeamsPlugin<McpPlugin>();
    }

    public static IMcpServerBuilder AddTeamsMcp(this IHostApplicationBuilder builder, McpServerOptions options)
    {
        builder.AddTeamsPlugin<McpPlugin>();

        return builder.Services.AddMcpServer((defaultOptions) =>
        {
            if (options is null) return;
            defaultOptions.Capabilities = options.Capabilities;
            defaultOptions.InitializationTimeout = options.InitializationTimeout;
            defaultOptions.ProtocolVersion = options.ProtocolVersion;
            defaultOptions.ScopeRequests = options.ScopeRequests;
            defaultOptions.ServerInfo = options.ServerInfo;
            defaultOptions.ServerInstructions = options.ServerInstructions;
        });
    }
}