using CompatBot;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Core;

// using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Core.Compat;
using Microsoft.Bot.Schema;

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

app.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpRequest request, HttpResponse response, CancellationToken ct) =>
    await adapter.ProcessAsync(request, response, bot, ct));

app.MapGet("/api/notify/{cid}", async (IBotFrameworkHttpAdapter adapter, string cid, CancellationToken ct) =>
{
    Activity proactive = new()
    {
        Conversation = new() { Id = cid },
        ServiceUrl = "https://smba.trafficmanager.net/teams"
    };
    await ((CompatAdapter)adapter).ContinueConversationAsync(
        string.Empty,
        proactive.GetConversationReference(),
        async (turnContext, ct) =>
        {
            await turnContext.SendActivityAsync($"Proactive Message send from SDK `{BotApplication.Version}` at {DateTime.Now:T}");
        },
        ct);
});

app.Run();
