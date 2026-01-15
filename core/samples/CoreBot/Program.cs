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

    replyText += $"<br /> Received Activity `{activity.Type}`.";

    //activity.Properties.Where(kvp => kvp.Key.StartsWith("text")).ToList().ForEach(kvp =>
    //{
    //    replyText += $"<br /> {kvp.Key}:`{kvp.Value}` ";
    //});


    string? conversationType = "unknown conversation type";
    if (activity.Conversation.Properties.TryGetValue("conversationType", out object? ctProp))
    {
        conversationType = ctProp?.ToString();
    }

    replyText += $"<br /> To  conv type: `{conversationType}` conv id: `{activity.Conversation.Id}`";

    CoreActivity replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithConversationReference(activity)
        .WithProperty("text", replyText)
        .Build();

    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();
