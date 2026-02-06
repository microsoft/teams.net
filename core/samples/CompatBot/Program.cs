// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Monitor.OpenTelemetry.AspNetCore;
using CompatBot;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Compat;
using Microsoft.Bot.Schema;

// using Microsoft.Bot.Connector.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry().UseAzureMonitor();
builder.AddCompatAdapter();

//builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
//builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(provider => 
//    new CloudAdapter(
//        provider.GetRequiredService<BotFrameworkAuthentication>(),
//        provider.GetRequiredService<ILogger<CloudAdapter>>()));


MemoryStorage storage = new();
ConversationState conversationState = new(storage);
builder.Services.AddSingleton(conversationState);
builder.Services.AddTransient<IBot, EchoBot>();

WebApplication app = builder.Build();

CompatAdapter compatAdapter = (CompatAdapter)app.Services.GetRequiredService<IBotFrameworkHttpAdapter>();
compatAdapter.Use(new MyCompatMiddleware());
compatAdapter.Use(new MyCompatMiddleware());

app.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpRequest request, HttpResponse response, CancellationToken ct) =>
    await adapter.ProcessAsync(request, response, bot, ct));

app.MapGet("/api/notify/{cid}", async (IBotFrameworkHttpAdapter adapter, string cid, CancellationToken ct) =>
{
    Activity proactive = new()
    {
        Conversation = new() { Id = cid },
        ServiceUrl = "https://smba.trafficmanager.net/teams"
    };
    await ((BotAdapter)adapter).ContinueConversationAsync(
        string.Empty,
        proactive.GetConversationReference(),
        async (turnContext, ct) =>
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text($"Proactive.  <br/> SDK `{BotApplication.Version}` at {DateTime.Now:T}"), ct);
        },
        ct);
});

app.Run();
