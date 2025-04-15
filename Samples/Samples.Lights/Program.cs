using System.Text.Json;

using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.Lights;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools().AddOpenAI<LightsPrompt>();

var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage("/history", async context =>
{
    var state = State.From(context);
    await context.Send(JsonSerializer.Serialize(state.Messages, new JsonSerializerOptions()
    {
        WriteIndented = true
    }));
});

teams.OnMessage(async context =>
{
    var state = State.From(context);
    var prompt = app.Services.GetOpenAIChatPrompt();

    await prompt.Send(context.Activity.Text, new() { Messages = state.Messages }, (chunk) => Task.Run(() =>
    {
        context.Stream.Emit(chunk);
    }), context.CancellationToken);

    state.Save(context);
});

app.Run();