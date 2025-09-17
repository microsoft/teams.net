using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.Lights;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<Controller>();
builder.AddTeams().AddTeamsDevTools().AddOpenAI<LightsPrompt>();

var app = builder.Build();

app.UseTeams();
app.Run();