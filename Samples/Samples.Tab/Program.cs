using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Teams.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();

app.AddTeamsTab("test", "Web/dist").UseTeams();
app.Run();