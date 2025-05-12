using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Prompts;

using ModelContextProtocol.Server;

namespace Microsoft.Teams.Plugins.External.Mcp.Extensions;

public static class McpServerExtensions
{
    public static IMcpServerBuilder WithTeamsChatPrompts(this IMcpServerBuilder builder)
    {
        var provider = builder.Services.BuildServiceProvider();
        var prompts = provider.GetServices<IChatPrompt>();

        List<McpServerPrompt> mcpPrompts = [];
        List<McpServerTool> mcpTools = [];

        foreach (var prompt in prompts)
        {
            var mcpPrompt = McpServerPrompt.Create(async (string text) =>
            {
                var res = await prompt.Send(UserMessage.Text(text));
                return ((ModelMessage<string>)res).Content;
            }, new()
            {
                Name = prompt.Name,
                Description = prompt.Description,
                Services = provider
            });

            mcpPrompts.Add(mcpPrompt);

            foreach (var (name, func) in prompt.Functions)
            {
                var fullname = $"{prompt.Name}.{name}";
                var fn = (Function)func;
                var mcpTool = McpServerTool.Create(fn.Handler, new()
                {
                    Title = fullname,
                    Name = fullname,
                    Description = fn.Description,
                    Services = provider
                });

                mcpTools.Add(mcpTool);
            }
        }

        builder.WithPrompts(mcpPrompts).WithTools(mcpTools);
        return builder;
    }
}