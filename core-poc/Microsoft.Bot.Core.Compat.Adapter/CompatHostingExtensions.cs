using Microsoft.Extensions.Hosting;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Microsoft.Bot.Core.Compat.Adapter;

public static class CompatHostingExtensions
{
    public static IHostApplicationBuilder AddCompatAdapter(this IHostApplicationBuilder builder)
    {
        builder.Services.AddBotApplication<BotApplication>();
        builder.Services.AddSingleton<CompatBotAdapter>();
        builder.Services.AddSingleton<IBotFrameworkHttpAdapter, CompatAdapter>();
        return builder;
    }
}
