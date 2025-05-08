using System.Text.Json;

using Json.More;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Common.Extensions;

using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace Microsoft.Teams.Plugins.External.Mcp.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IMcpServerBuilder AddTeamsMcp(this IHostApplicationBuilder builder, McpServerOptions? options = null)
    {
        var provider = builder.Services.BuildServiceProvider();
        var prompts = provider.GetServices<IChatPrompt>();

        builder.AddTeamsPlugin<McpPlugin>();

        var mcp = builder.Services.AddMcpServer((defaultOptions) =>
        {
            if (options is null) return;
            defaultOptions.Capabilities = options.Capabilities;
            defaultOptions.InitializationTimeout = options.InitializationTimeout;
            defaultOptions.ProtocolVersion = options.ProtocolVersion;
            defaultOptions.ScopeRequests = options.ScopeRequests;
            defaultOptions.ServerInfo = options.ServerInfo;
            defaultOptions.ServerInstructions = options.ServerInstructions;
        });

        mcp.WithListToolsHandler((context, _) =>
        {
            return ValueTask.FromResult(new ListToolsResult()
            {
                Tools = prompts
                    .SelectMany(prompt => prompt.Functions.Values.Select(func => (prompt, func)).ToArray())
                    .Select(item => new Tool()
                    {
                        Name = $"{item.prompt.Name}.{item.func.Name}",
                        Description = item.func.Description,
                        InputSchema = item.func.Parameters is null
                            ? default
                            : item.func.Parameters.ToJsonDocument().RootElement
                    })
                    .ToList()
            });
        });

        mcp.WithCallToolHandler(async (context, cancellationToken) =>
        {
            try
            {
                if (context.Params is null) throw new InvalidDataException();

                var parts = context.Params.Name.Split(".", 2);
                var prompt = prompts.Where(p => p.Name == parts[0]).FirstOrDefault() ?? throw new InvalidOperationException();
                var method = prompt.GetType().GetMethod("Invoke") ?? throw new Exception("invoke method not found");
                var call = new FunctionCall()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = parts[1],
                    Arguments = JsonSerializer.Serialize(context.Params.Arguments)
                };

                var res = await method.InvokeAsync(prompt, [call, cancellationToken]);

                return new()
                {
                    Content = [
                        new()
                        {
                            Type = "text",
                            Text = res is string asString ? asString : JsonSerializer.Serialize(res)
                        }
                    ]
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    IsError = true,
                    Content = [
                        new()
                        {
                            Type = "text",
                            Text = ex.ToString()
                        }
                    ]
                };
            }
        });

        return mcp;
    }
}