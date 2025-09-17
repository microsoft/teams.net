using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Teams.Plugins.External.McpClient;

using Samples.McpClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<Controller>().AddHttpContextAccessor();
builder.Services.AddSingleton((sp) => new McpClientPlugin().UseMcpServer("https://learn.microsoft.com/api/mcp"));
builder.AddTeams().AddTeamsDevTools().AddOpenAI<DocsPrompt>();


var app = builder.Build();

app.UseTeams();
app.Run();