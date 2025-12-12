// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;

BotApplicationBuilder botAppBuilder = BotApplication.CreateBuilder();
botAppBuilder.WithRoutePath("/api/messages");
botAppBuilder.Services.AddOpenTelemetry().UseAzureMonitor();
BotApplication botApp = botAppBuilder.Build();

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string replyText = $"CoreBot running on SDK {BotApplication.Version}.";
    replyText += $"\r\nYou sent: `{activity.Text}` in activity of type `{activity.Type}`.";

    string? conversationType = "unknown conversation type";
    if (activity.Conversation.Properties.TryGetValue("conversationType", out object? ctProp))
    {
        conversationType = ctProp?.ToString();
    }

    replyText += $"\r\n to Conversation ID: `{activity.Conversation.Id}` conv type: `{conversationType}`";
    CoreActivity replyActivity = activity.CreateReplyMessageActivity(replyText);
    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

botApp.Run();
