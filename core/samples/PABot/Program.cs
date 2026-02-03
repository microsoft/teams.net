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

builder.Services.AddKeyedSingleton<IBotFrameworkHttpAdapter>("RidoABSOne", (sp, keyName) =>
{
    return new AdapterWithErrorHandler(
        sp,
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<ILogger<IBotFrameworkHttpAdapter>>(),
        sp.GetRequiredService<IStorage>(),
        sp.GetRequiredService<ConversationState>(),
        "RidoABSOne");
});

builder.Services.AddKeyedSingleton<IBotFrameworkHttpAdapter>("RidoABSTwo", (sp, keyName) =>
{
    return new AdapterWithErrorHandler(
        sp,
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<ILogger<IBotFrameworkHttpAdapter>>(),
        sp.GetRequiredService<IStorage>(),
        sp.GetRequiredService<ConversationState>(),
        "RidoABSTwo");
});

builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<UserState>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddSingleton<MainDialog>();
//builder.Services.AddKeyedTransient<IBot, TeamsBot<MainDialog>>("TeamsBot");
builder.Services.AddKeyedTransient<IBot, EchoBot>("TeamsBot");
builder.Services.AddKeyedTransient<IBot, EchoBot>("EchoBot");
var app = builder.Build();

var adapterOne = app.Services.GetRequiredKeyedService<IBotFrameworkHttpAdapter>("RidoABSOne");
var adapterTwo = app.Services.GetRequiredKeyedService<IBotFrameworkHttpAdapter>("RidoABSTwo");

app.MapPost("/api/ridoabsone", (HttpRequest request, HttpResponse response, [FromKeyedServices("TeamsBot")]IBot bot, CancellationToken ct) =>
    adapterOne.ProcessAsync(request, response, bot, ct)).RequireAuthorization("RidoABSOne");

app.MapPost("/api/ridoabstwo", (HttpRequest request, HttpResponse response, [FromKeyedServices("EchoBot")]IBot bot, CancellationToken ct) =>
    adapterTwo.ProcessAsync(request, response, bot, ct)).RequireAuthorization("RidoABSTwo");

app.Run();
