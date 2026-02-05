using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.BotBuilder;

var builder = WebApplication.CreateBuilder(args);
builder
    .AddTeams()
    .AddTeamsDevTools()
    .AddBotBuilder<Bot, BotBuilderAdapter, ConfigurationBotFrameworkAuthentication>();

var app = builder.Build();

var teams = app.UseTeams();

teams.OnMessage(async context =>
{
    await context.Typing();
    await context.Send("hi from teams...");
});

app.Run();