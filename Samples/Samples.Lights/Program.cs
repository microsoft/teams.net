using System.Text.Json;

using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Models.OpenAI.Extensions;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.Lights;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools().AddOpenAI<LightsPrompt>();

var app = builder.Build();

// Get the prompt factory from services
var promptFactory = app.Services.GetRequiredService<Func<OpenAIChatPrompt>>();

var teams = app.UseTeams();

teams.OnMessage("/history", async (context, cancellationToken) =>
{
    var state = State.From(context);
    await context.Send(JsonSerializer.Serialize(state.Messages, new JsonSerializerOptions()
    {
        WriteIndented = true
    }), cancellationToken);
});

teams.OnMessage(async (context, cancellationToken) =>
{
    var state = State.From(context);
    var prompt = promptFactory();
    await prompt.Send(context.Activity.Text, new() { Messages = state.Messages }, (chunk) => Task.Run(() =>
    {
        context.Stream.Emit(chunk);
    }), cancellationToken);

    state.Save(context);
});

app.Run();
