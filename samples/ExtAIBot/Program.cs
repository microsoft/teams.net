// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Anthropic;
using Anthropic.Core;
using Azure.AI.OpenAI;
using ExtAIBot;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;

// Wires up the Teams bot application. Handler registration lives in ExtAIBotApp.

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication<ExtAIBotApp>();

builder.Services.AddSingleton<IChatClient>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    string provider = config["AI_PROVIDER"] ?? "azure-openai";
    IChatClient client = provider switch
    {
        "anthropic" => CreateAnthropicClient(config),
        "azure-openai" => CreateAzureOpenAIClient(config),
        _ => throw new InvalidOperationException(
            $"Unsupported AI_PROVIDER '{provider}'. Use 'azure-openai' or 'anthropic'."
        ),
    };

    return client
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
});

builder.Services.AddSingleton<McpToolSetLifetimeService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<McpToolSetLifetimeService>());

builder.Services.AddSingleton<Agent>();

WebApplication webApp = builder.Build();
webApp.UseTeamsBotApplication<ExtAIBotApp>();
webApp.Run();

static IChatClient CreateAnthropicClient(IConfiguration config)
{
    string apiKey = config["ANTHROPIC_API_KEY"] ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is required.");
    string model = config["ANTHROPIC_MODEL"] ?? throw new InvalidOperationException("ANTHROPIC_MODEL is required.");
    return new AnthropicClient(new ClientOptions { ApiKey = apiKey }).AsIChatClient(model);
}

static IChatClient CreateAzureOpenAIClient(IConfiguration config)
{
    string endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
    string apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
    string deployment = config["AzureOpenAI:Deployment"] ?? throw new InvalidOperationException("AzureOpenAI:Deployment is required.");

    return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
        .GetChatClient(deployment)
        .AsIChatClient();
}
