

using Microsoft.Bot.Core.Hosting;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBotApplication<App>();
// builder.AddTeams();
var app = builder.Build();
var teamsApp = app.UseBotApplication<App>();
// var teamsApp = app.UseTeams();

teamsApp.OnMessage(context =>
{
    return context.Send("msg received");
});

app.Run();