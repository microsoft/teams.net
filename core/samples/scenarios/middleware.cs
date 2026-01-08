#!/usr/bin/dotnet run

#:sdk Microsoft.NET.Sdk.Web

#:project ../../src/Microsoft.Bot.Core/Microsoft.Bot.Core.csproj

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Schema;
using Microsoft.Bot.Core.Hosting;


WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication<BotApplication>();
WebApplication webApp = webAppBuilder.Build();
var botApp = webApp.UseBotApplication<BotApplication>();

botApp.Use(new MyTurnMiddleWare());

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string? text = activity.Properties.TryGetValue("text", out object? value) ? value?.ToString() : null;
    var replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityTypes.Message)
        .WithConversationReference(activity)
        .WithProperty("text", "You said " + text)
        .Build();
    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();

public class MyTurnMiddleWare : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn next, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"MIDDLEWARE: Processing activity {activity.Type} {activity.Id}");
        return next(cancellationToken);
    }
}