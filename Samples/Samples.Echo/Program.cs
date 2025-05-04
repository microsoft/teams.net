using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Routing;
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

    [ActivityController]
    public class Controller
    {
        [Activity]
        public async Task OnActivity(IContext<Activity> context, [Context] IContext.Next next)
        {
            context.Log.Info(context.AppId);
            await next();
        }

        [Message]
        public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
        {
            await client.Typing();
            await client.Send($"you said '{activity.Text}'");
        }
    }
}