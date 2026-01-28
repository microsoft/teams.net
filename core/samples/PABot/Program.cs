// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using PABot;
using PABot.Bots;
using PABot.Dialogs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCustomCompatAdapter();
builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<MainDialog>();
builder.Services.AddTransient<IBot, TeamsBot<MainDialog>>();
//builder.Services.AddTransient<IBot, EchoBot>();
//builder.Services.AddTransient<IBot, SsoBot>();
var app = builder.Build();

var adapter = app.Services.GetRequiredService<IBotFrameworkHttpAdapter>();

app.MapPost("/api/messages", (HttpRequest request, HttpResponse response, IBot bot, CancellationToken ct) =>
    adapter.ProcessAsync(request, response, bot, ct)).RequireAuthorization();

app.Run();
