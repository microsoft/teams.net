using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams(App.Builder().AddLogger(level: Microsoft.Teams.Common.Logging.LogLevel.Debug));

var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage("/signout", async context =>
{
    if (!context.IsSignedIn)
    {
        await context.Send("you are not signed in!");
        return;
    }

    await context.SignOut();
    await context.Send("you have been signed out!");
});

teams.OnMessage(async context =>
{
    if (!context.IsSignedIn)
    {
        await context.SignIn();
        return;
    }

    var me = await context.UserGraph.Me.GetAsync();
    await context.Send($"user '{me!.DisplayName}' is signed in!");
});

app.Run();