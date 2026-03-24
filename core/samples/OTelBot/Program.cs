using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Core;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

webAppBuilder.Services.AddOpenTelemetry()
    .WithTracing( tracing => tracing.AddSource(BotCoreTelemetry.ActivitySource.Name).AddAspNetCoreInstrumentation())
    .WithMetrics( metrics => metrics.AddMeter(BotCoreMetrics.Meter.Name).AddAspNetCoreInstrumentation())
    .UseAzureMonitor();
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage(async (turnContext, cancellationToken) =>
{
    string userMessage = turnContext.Activity.Text ?? "no text found";
    string responseMessage = $"You said: {userMessage}";
    await turnContext.SendActivityAsync(responseMessage, cancellationToken);
});


webApp.Run();
