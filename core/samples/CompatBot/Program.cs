// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CompatBot;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
// using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Core.Compat;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

app.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpRequest request, HttpResponse response) =>
    await adapter.ProcessAsync(request, response, bot));

app.Run();
