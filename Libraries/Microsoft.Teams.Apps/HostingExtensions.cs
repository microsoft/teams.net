
using Microsoft.AspNetCore.Builder;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Teams.Apps;

public static class HostingExtensions
{
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder)
    {
        builder.Services.AddBotApplication<App>();
        return builder;
    }

    public static App UseTeams(this IApplicationBuilder appBuilder)
    {
        return appBuilder.UseBotApplication<App>();
        
    }
}
