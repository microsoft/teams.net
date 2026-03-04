// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.AI;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using OpenAI;

var builder = TeamsBotApplication.CreateBuilder(args);

string apiKey  = builder.Configuration["OpenAI:ApiKey"]  ?? throw new InvalidOperationException("OpenAI:ApiKey is required.");
string modelId = builder.Configuration["OpenAI:ModelId"] ?? "gpt-4o-mini";

IChatClient? chatClient = new OpenAIClient(apiKey)
    .GetChatClient(modelId)
    .AsIChatClient();

var teamsApp = builder.Build();

teamsApp.OnMessage("(?i)stream", async (context, cancellationToken) =>
{
    ActivityStreamingWriter writer = context.GetStreamingWriter();
    await writer.SendInformativeAsync("Thinking…", cancellationToken);

    string userText = context.Activity.Text ?? "Tell me something interesting.";

    await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(
        [new ChatMessage(ChatRole.User, userText)],
        cancellationToken: cancellationToken))
    {
        if (!string.IsNullOrEmpty(update.Text))
            await writer.AppendAsync(update.Text, cancellationToken);
    }

    await writer.FinalizeAsync(cancellationToken);
});

teamsApp.Run();
