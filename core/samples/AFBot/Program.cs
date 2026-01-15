// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using AFBot;
using Azure.AI.OpenAI;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Agents.AI;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using OpenAI;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddOpenTelemetry().UseAzureMonitor();
webAppBuilder.Services.AddBotApplication<BotApplication>();
WebApplication webApp = webAppBuilder.Build();
BotApplication botApp = webApp.UseBotApplication<BotApplication>();

AzureOpenAIClient azureClient = new(
           new Uri("https://tsdkfoundry.openai.azure.com/"),
           new ApiKeyCredential(Environment.GetEnvironmentVariable("AZURE_OpenAI_KEY")!));

ChatClientAgent agent = azureClient.GetChatClient("gpt-5-nano").CreateAIAgent(
    instructions: "You are an expert acronym maker, made an acronym made up from the first three characters of the user's message. " +
                    "Some examples: OMW on my way, BTW by the way, TVM thanks very much, and so on." +
                    "Always respond with the three complete words only, and include a related emoji at the end.",
    name: "AcronymMaker");

botApp.Use(new DropTypingMiddleware());

botApp.OnActivity = async (activity, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(activity);

    CancellationTokenSource timer = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token);

    CoreActivity typing = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Typing)
        .WithConversationReference(activity)
        .Build();
    await botApp.SendActivityAsync(typing, cancellationToken);

    AgentRunResponse agentResponse = await agent.RunAsync(activity.Properties["text"]?.ToString() ?? "OMW", cancellationToken: timer.Token);

    var m1 = agentResponse.Messages.FirstOrDefault();
    Console.WriteLine($"AI:: GOT {agentResponse.Messages.Count} msgs");
    CoreActivity replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithConversationReference(activity)
        .WithProperty("text", m1!.Text)
        .Build();

    var res = await botApp.SendActivityAsync(replyActivity, cancellationToken);

    Console.WriteLine("SENT >>> => " + res?.Id);
};

webApp.Run();
