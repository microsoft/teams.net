using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.Apps.Extensions;

using ModelContextProtocol.Server;

namespace Microsoft.Teams.Plugins.AspNetCore.Mcp.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IMcpServerBuilder AddTeamsMcp(this IHostApplicationBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        builder.AddTeamsPlugin<McpPlugin>();
        
        var mcp = builder.Services.AddMcpServer();
        
        foreach (var prompt in provider.GetServices<IChatPrompt>())
        {
            foreach (var (name, func) in prompt.Functions)
            {
                if (func is Function function)
                {
                    mcp = mcp.WithTools([
                        McpServerTool.Create(function.Invoke, new()
                        {
                            Name = func.Name,
                            Description = func.Description,
                            Services = provider
                        })
                    ]);
                }
            }
        }

        return mcp;
    }
}