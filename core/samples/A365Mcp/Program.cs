// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using A365Mcp;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;

// Wires up the Teams bot application and delegates AI execution to Agent.
// Handler registration lives in TeamsBotAppHandlers.cs.

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();

builder.Services.AddSingleton<IChatClient>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    string endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
    string apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
    string modelId = config["AzureOpenAI:ModelId"] ?? throw new InvalidOperationException("AzureOpenAI:ModelId is required.");

    return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
        .GetChatClient(modelId)
        .AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();
});


builder.Services.AddSingleton<IMcpClientFactory, McpClientFactory>();
builder.Services.AddSingleton<Agent>();

WebApplication webApp = builder.Build();

Agent agent = webApp.Services.GetRequiredService<Agent>();
ILogger handlerLogger = webApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("A365Mcp.TeamsBotAppHandlers");

webApp.UseTeamsBotApplication().RegisterHandlers(agent, handlerLogger);

webApp.Run();
