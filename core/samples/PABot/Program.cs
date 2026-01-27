// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Teams.Bot.Compat;
using PABot;
using PABot.Bots;
using PABot.Dialogs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCompatAdapter();

builder.Services.AddHttpClient();

builder.Services.AddRouting();

// Create the Bot Framework Adapter with error handling enabled.
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

// Create the Bot Framework Authentication to be used with the Bot Adapter.
builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

// Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
builder.Services.AddSingleton<IStorage, MemoryStorage>();

// Create the User state. (Used in this bot's Dialog implementation.)
builder.Services.AddSingleton<UserState>();

// Create the Conversation state. (Used by the Dialog system itself.)
builder.Services.AddSingleton<ConversationState>();

// The Dialog that will be run by the bot.
builder.Services.AddSingleton<MainDialog>();

// Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
builder.Services.AddTransient<IBot, TeamsBot<MainDialog>>();

var app = builder.Build();

// Configure the HTTP request pipeline.l

var adapter = app.Services.GetRequiredService<IBotFrameworkHttpAdapter>();

app.MapPost("/api/messages", (HttpRequest request, HttpResponse response, IBot bot, CancellationToken ct) =>
    adapter.ProcessAsync(request, response, bot, ct)).RequireAuthorization();

app.Run();
