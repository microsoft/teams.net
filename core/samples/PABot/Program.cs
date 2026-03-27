// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Teams.Bot.Core;
using PABot;
using PABot.Bots;
using PABot.Dialogs;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Register TeamsBotApplication and all dependencies (uses MsalBot and MsalAgent configuration sections)
builder.Services.AddTeamsBotApplications();

// Register adapter using the TeamsBotApplication
builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(sp =>
{
    return new AdapterWithErrorHandler(
        sp.GetRequiredService<BotApplication>(),
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<IBotFrameworkHttpAdapter>>(),
        sp.GetRequiredService<IStorage>(),
        sp.GetRequiredService<ConversationState>());
});

// Register bot state and dialog
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<MainDialog>();

// Register bot (pick between TeamsBot & EchoBot)
//builder.Services.AddTransient<IBot, TeamsBot<MainDialog>>();
builder.Services.AddTransient<IBot, EchoBot>();

WebApplication app = builder.Build();

// Map endpoint with BotAdapter authorization policy
app.MapPost("/api/messages", (HttpRequest request, HttpResponse response, IBot bot, CancellationToken ct, IBotFrameworkHttpAdapter adapter) =>
    adapter.ProcessAsync(request, response, bot, ct)).RequireAuthorization("BotAdapter");

app.Run();
