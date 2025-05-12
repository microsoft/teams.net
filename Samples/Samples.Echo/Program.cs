using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Samples.Echo;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddTransient<Controller>();
        builder.AddTeams().AddTeamsDevTools();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseTeams();
        app.Run();
    }

    [TeamsController]
    public class Controller
    {
        [Activity]
        public async Task OnActivity(IContext<Activity> context, [Context] IContext.Next next)
        {
            context.Log.Info(context.AppId);
            await next();
        }

        [Message]
        public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("hit!");
            await client.Typing();
            await client.Send($"you said '{activity.Text}'");
        }

        [Microsoft.Teams.Apps.Events.Event("activity")]
        public void OnEvent(IPlugin plugin, Microsoft.Teams.Apps.Events.Event @event)
        {
            Console.WriteLine("!!HIT!!");
        }
    }
}