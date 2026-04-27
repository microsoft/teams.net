// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication();
WebApplication webApp = webAppBuilder.Build();

webApp.MapGet("/", () => "CoreBot is running.");
BotApplication botApp = webApp.UseBotApplication();

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string replyText = $"CoreBot running on SDK `{BotApplication.Version}`.";

    string conversationId = activity.Properties.Extract<Conversation>("conversation")?.Id
        ?? throw new InvalidOperationException("Conversation ID not found");

    CoreActivity replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithServiceUrl(activity.ServiceUrl!)
        .WithChannelId(activity.ChannelId!)
        .WithProperty("conversation", activity.Properties["conversation"])
        .WithProperty("from", activity.Properties["recipient"])
        .WithProperty("text", replyText)
        .Build();

    await botApp.SendActivityAsync(replyActivity, conversationId, cancellationToken: cancellationToken);
};

webApp.Run();
