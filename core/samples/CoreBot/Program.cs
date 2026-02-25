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

    CoreActivity replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithConversationReference(activity)
        .WithProperty("text", replyText)
        .Build();

    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();
