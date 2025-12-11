using CompatBot;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
// using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Core.Compat;

var builder = WebApplication.CreateBuilder(args);

builder.AddCompatAdapter();

//builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
//builder.Services.AddSingleton<IBotFrameworkHttpAdapter>(provider => 
//    new CloudAdapter(
//        provider.GetRequiredService<BotFrameworkAuthentication>(),
//        provider.GetRequiredService<ILogger<CloudAdapter>>()));


var storage = new MemoryStorage();
var conversationState = new ConversationState(storage);
builder.Services.AddSingleton(conversationState);
builder.Services.AddTransient<IBot, EchoBot>();

var app = builder.Build();

app.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpRequest request, HttpResponse response) =>
    await adapter.ProcessAsync(request, response, bot));

app.Run();
