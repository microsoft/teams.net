using Microsoft.Teams.Extensions.Logging;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();

app.UseTeams();
app.AddTeamsTab("test", "Web/bin");
app.Run();