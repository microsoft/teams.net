using System.Collections.Concurrent;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Compat.Adapter;
using Microsoft.Bot.Schema;

using Microsoft.Bot.Core.Hosting;
using compat;

WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);
webAppBuilder.Services.AddBotApplication<BotApplication>();

webAppBuilder.Services.AddSingleton<CompatBotAdapter>();
webAppBuilder.Services.AddSingleton<IBotFrameworkHttpAdapter, CustomAdapter>();
webAppBuilder.Services.AddSingleton<IBot, EchoBot>();

webAppBuilder.Services.AddSingleton<ConcurrentDictionary<string, ConversationReference>>();

WebApplication webApp = webAppBuilder.Build();

webApp.MapPost("/api/messages", async (IBotFrameworkHttpAdapter adapter, IBot bot, HttpRequest request, HttpResponse response) =>
    await adapter.ProcessAsync(request, response, bot));

webApp.MapGet("/api/notify", async (HttpRequest request, HttpResponse response) =>
{
    IBotFrameworkHttpAdapter adapter = webApp.Services.GetRequiredService<IBotFrameworkHttpAdapter>();
    ConversationReference? convRef = webApp.Services.GetRequiredService<ConcurrentDictionary<string, ConversationReference>>().Values.FirstOrDefault();
    await ((CompatBotAdapter)adapter).ContinueConversationAsync(
        webApp.Configuration["MicrosoftAppId"]!,
        convRef!,
        async (turnContext, cancellationToken) =>
        {
            await turnContext.SendActivityAsync("This is a proactive notification.", null, null, cancellationToken);
        },
        default);
});

webApp.Run();
