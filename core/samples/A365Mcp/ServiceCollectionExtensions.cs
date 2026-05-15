// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;

namespace A365Mcp;

/// <summary>
/// Extension methods for registering the Agent, its dependencies, and the
/// custom <see cref="A365TeamsBotApp"/> with DI.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="A365TeamsBotApp"/>, <see cref="Agent"/>, <see cref="IConversationHistoryStore"/>,
    /// <see cref="IMcpClientFactory"/>, and <see cref="IChatClient"/> with the service collection.
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

        // Conversation history must outlive any single turn -> singleton.
        services.AddSingleton<IConversationHistoryStore, InMemoryConversationHistoryStore>();

        // Agent is a per-turn execution unit; resolved from a fresh scope inside the bot handler.
        services.AddScoped<Agent>();

        // Register the custom Teams bot subclass so handlers are wired via constructor injection
        // instead of via a static service-locator extension method.
        services.AddTeamsBotApplication<A365TeamsBotApp>();

        return services;
    }
}
