// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AFCompat;
using Azure.Monitor.OpenTelemetry.AspNetCore;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Core.Compat;

// using Microsoft.Bot.Connector.Authentication;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry().UseAzureMonitor();
builder.AddCompatAdapter();
MemoryStorage storage = new();
ConversationState conversationState = new(storage);
builder.Services.AddSingleton(conversationState);
builder.Services.AddTransient<IBot, StreamingBot>();

WebApplication app = builder.Build();


app.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpRequest request, HttpResponse response, CancellationToken ct) =>
    await adapter.ProcessAsync(request, response, bot, ct));



    app.Run();
