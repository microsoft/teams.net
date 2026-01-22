using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Deprecated.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
builder.AddTeamsDevTools();
#pragma warning disable CS0612 // Type or member is obsolete
builder.Services.AddTransient<MainController>();
#pragma warning restore CS0612 // Type or member is obsolete

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseTeams();
app.Run();