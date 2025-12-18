// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Teams.BotApps;
using OpenAI;


AzureOpenAIClient azureClient = new(
           new Uri("https://ridofoundry.cognitiveservices.azure.com/"),
           new ApiKeyCredential(Environment.GetEnvironmentVariable("AZURE_OpenAI_KEY")!));

ChatClientAgent agent = azureClient.GetChatClient("gpt-5-nano").CreateAIAgent(
    name: "AcronymMaker",
    instructions: "You are an expert acronym maker, made an acronym made up from the first three characters of the user's message. " +
                  "Some examples: OMW on my way, BTW by the way, TVM thanks very much, and so on." +
                  "Always respond with the three complete words only, and include a related emoji at the end.");
    

var app = TeamsBotApplication.CreateBuilder().Build();

app.OnMessage = async (context, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(context);

    await context.SendTypingActivityAsync(cancellationToken);

    CancellationTokenSource timer = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    AgentRunResponse agentResponse = await agent.RunAsync(context.Activity.Text ?? "OMW", cancellationToken:  timer.Token);
    
    agentResponse.Messages.ToList().ForEach(async message =>
    {
        if (message.Role == ChatRole.Assistant && message.Contents is not null)
        {
            await context.SendActivityAsync(message.Text, cancellationToken);
        }
    });

};

app.Run();
