// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.DevTools;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication();
webAppBuilder.Services.AddDevTools();
WebApplication webApp = webAppBuilder.Build();

webApp.MapGet("/", () => "CoreBot is running.");
BotApplication botApp = webApp.UseBotApplication();
webApp.UseDevTools();

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
