using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.Mcp.Prompts;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools().AddOpenAI<MainPrompt>();

var app = builder.Build();

app.UseTeams();
app.Run();