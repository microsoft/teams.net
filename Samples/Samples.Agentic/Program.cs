using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.Agentic;

var builder = WebApplication.CreateBuilder(args);

var appBuilder = App.Builder().AddCredentials(new AgenticCredentials(builder.Configuration));
builder.AddTeams(appBuilder);
var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage(async context =>
{
   await context.Send("Hello World!");  
});

app.Run();