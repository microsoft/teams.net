// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Teams.Bot.Apps;
using PABot;
using PABot.Bots;
using PABot.Dialogs;

var builder = WebApplication.CreateBuilder(args);

// Register all the keyed services (ConversationClient, UserTokenClient, TeamsApiClient, TeamsBotApplication)
builder.Services.AddTeamsBotApplications();

// Register keyed adapters using the keyed TeamsBotApplication
builder.Services.AddKeyedSingleton<IBotFrameworkHttpAdapter>("AdapterOne", (sp, keyName) =>
{
    return new AdapterWithErrorHandler(
        sp.GetRequiredKeyedService<TeamsBotApplication>("AdapterOne"),
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<IBotFrameworkHttpAdapter>>(),
        sp.GetRequiredService<IStorage>(),
        sp.GetRequiredService<ConversationState>());
});

builder.Services.AddKeyedSingleton<IBotFrameworkHttpAdapter>("AdapterTwo", (sp, keyName) =>
{
    return new AdapterWithErrorHandler(
        sp.GetRequiredKeyedService<TeamsBotApplication>("AdapterTwo"),
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

// Register bots
builder.Services.AddKeyedTransient<IBot, TeamsBot<MainDialog>>("TeamsBot");
builder.Services.AddKeyedTransient<IBot, EchoBot>("EchoBot");

var app = builder.Build();

// Get the keyed adapters
var adapterOne = app.Services.GetRequiredKeyedService<IBotFrameworkHttpAdapter>("AdapterOne");
var adapterTwo = app.Services.GetRequiredKeyedService<IBotFrameworkHttpAdapter>("AdapterTwo");

// Map endpoints with their respective adapters and authorization policies
app.MapPost("/api/messages", (HttpRequest request, HttpResponse response, [FromKeyedServices("EchoBot")]IBot bot, CancellationToken ct) =>
    adapterOne.ProcessAsync(request, response, bot, ct)).RequireAuthorization("AdapterOne");

app.MapPost("/api/v2/messages", (HttpRequest request, HttpResponse response, [FromKeyedServices("TeamsBot")]IBot bot, CancellationToken ct) =>
    adapterTwo.ProcessAsync(request, response, bot, ct)).RequireAuthorization("AdapterTwo");

app.Run();
