// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Azure.AI.OpenAI;
using ExtAIBot;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;

// Wires up the Teams bot application and delegates AI execution to Agent.
// Handler registration lives in Handlers.cs.

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

builder.Services.AddSingleton<McpToolSet>(sp =>
    McpToolSet.CreateAsync(sp.GetRequiredService<ILogger<McpToolSet>>()).GetAwaiter().GetResult());

builder.Services.AddSingleton<Agent>();

WebApplication webApp = builder.Build();

Agent agent = webApp.Services.GetRequiredService<Agent>();
McpToolSet mcpTools = webApp.Services.GetRequiredService<McpToolSet>();
ILogger handlerLogger = webApp.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ExtAIBot.Handlers");
webApp.Lifetime.ApplicationStopping.Register(() => _ = mcpTools.DisposeAsync());

webApp.UseTeamsBotApplication().RegisterHandlers(agent, handlerLogger);

webApp.Run();
