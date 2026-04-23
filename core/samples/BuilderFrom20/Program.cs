using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;


var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();  
var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage(async (ctx, ct) =>
{
    await ctx.Send("echo2: " + ctx.Activity.Text, ct);
});

app.Run();


/**
 * 

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
var appBuilder = App.Builder().AddOAuth("sso");

builder.AddTeams(appBuilder);
var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage(async (ctx, ct) =>
{
    await ctx.Send("echo2: " + ctx.Activity.Text, ct);
});

app.Run();

 * 
 */
