using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder();
builder.AddTeams();
var app = builder.Build();
var teamsApp = app.UseTeams();

teamsApp.OnMessage(async context =>
{
    // await context.Typing();
    await context.Send($"you said '{context.Activity.Text}'");
});

app.Run();
