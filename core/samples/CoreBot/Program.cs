// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddOpenTelemetry().UseAzureMonitor();
webAppBuilder.Services.AddBotApplication<BotApplication>();
WebApplication webApp = webAppBuilder.Build();
BotApplication botApp = webApp.UseBotApplication<BotApplication>();

webApp.MapGet("/", () => "CoreBot is running.");

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string replyText = $"CoreBot running on SDK {BotApplication.Version}.";
    replyText += $"<br /> You sent: `{activity.Text}` in activity of type `{activity.Type}`.";

    string? conversationType = "unknown conversation type";
    if (activity.Conversation.Properties.TryGetValue("conversationType", out object? ctProp))
    {
        conversationType = ctProp?.ToString();
    }

    replyText += $"<br /> To Conversation ID: `{activity.Conversation.Id}` conv type: `{conversationType}`";

    CoreActivity replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityTypes.Message)
        .WithConversationReference(activity)
        .WithText(replyText)
        .Build();

    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();
