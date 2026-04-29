// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication();
WebApplication webApp = webAppBuilder.Build();

webApp.MapGet("/", () => "CoreBot is running.");
BotApplication botApp = webApp.UseBotApplication();

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string replyText = $"CoreBot running on SDK `{BotApplication.Version}`.";
    ArgumentNullException.ThrowIfNull(activity.Conversation);
    CoreActivity replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithChannelId(activity.ChannelId)
        .WithServiceUrl(activity.ServiceUrl)
        .WithConversation(activity.Conversation)
        .WithFrom(activity.Recipient)
        .WithProperty("text", replyText)
        .Build();

    await botApp.SendActivityAsync(replyActivity, cancellationToken: cancellationToken);
};

webApp.Run();
