using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Samples.BotBuilder;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddTransient<Controller>();
        builder
            .AddTeams()
            .AddTeamsDevTools()
            .AddBotBuilder<Bot, BotBuilderAdapter, ConfigurationBotFrameworkAuthentication>();

        var app = builder.Build();

        app.UseTeams();
        app.Run();
    }

    [TeamsController]
    public class Controller
    {
        [Message]
        public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
        {
            await client.Typing();
            await client.Send($"hi from teams...");
        }
    }
}