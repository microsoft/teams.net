// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace A365Mcp;

/// <summary>
/// Extension methods for registering the Agent and its dependencies with DI.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="Agent"/>, <see cref="IMcpClientFactory"/>, and <see cref="IChatClient"/>
    /// with the service collection.
    /// </summary>
    public static IServiceCollection AddAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));

        services.AddChatClient(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            string endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
            string apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
            string modelId = config["AzureOpenAI:ModelId"] ?? throw new InvalidOperationException("AzureOpenAI:ModelId is required.");

            return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
                .GetChatClient(modelId)
                .AsIChatClient();
        })
        .UseFunctionInvocation();

        services.AddSingleton<IMcpClientFactory, McpClientFactory>();
        services.AddSingleton<Agent>();

        return services;
    }
}
