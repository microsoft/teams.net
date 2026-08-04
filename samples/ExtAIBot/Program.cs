// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
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
    string endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
    string apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
    string deployment = config["AzureOpenAI:Deployment"] ?? throw new InvalidOperationException("AzureOpenAI:Deployment is required.");

    return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
        .GetChatClient(deployment)
        .AsIChatClient()
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
