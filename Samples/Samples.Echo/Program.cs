using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();

teams.OnActivity(async (context, cancellationToken) =>
{
    context.Log.Info(context.AppId);
    await context.Next();
});

teams.OnMessage(async (context, cancellationToken) =>
{
    context.Log.Info("hit!");
    await context.Typing("processing your response", cancellationToken);
    await context.Send($"you said '{context.Activity.Text}'", cancellationToken);
});

app.Run();