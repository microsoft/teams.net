#!/usr/bin/dotnet run

#:sdk Microsoft.NET.Sdk.Web

#:project ../../src/Microsoft.Teams.Bot.Core/Microsoft.Teams.Bot.Core.csproj

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Core.Hosting;


WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Register middleware that needs dependencies
webAppBuilder.Services.AddTransient<LoggingMiddleware>();

webAppBuilder.Services.AddBotApplication<BotApplication>();
WebApplication webApp = webAppBuilder.Build();
var botApp = webApp.UseBotApplication<BotApplication>();

// Pattern 1: Direct instantiation (for simple middleware without dependencies)
botApp.Use(new SimpleMiddleware());

// Pattern 2: DI resolution (for middleware with dependencies)
botApp.UseMiddleware<LoggingMiddleware>(webApp.Services);

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string? text = activity.Properties.TryGetValue("text", out object? value) ? value?.ToString() : null;
    var replyActivity = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithConversationReference(activity)
        .WithProperty("text", "You said " + text)
        .Build();
    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();

// Simple middleware without dependencies - use direct instantiation
public class SimpleMiddleware : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn next, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[SIMPLE] Processing activity {activity.Type} {activity.Id}");
        return next(cancellationToken);
    }
}

// Middleware with dependencies - register in DI and use UseMiddleware
public class LoggingMiddleware : ITurnMiddleWare
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn next, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[LOGGING] Processing activity {Type} {Id}", activity.Type, activity.Id);
        return next(cancellationToken);
    }
}