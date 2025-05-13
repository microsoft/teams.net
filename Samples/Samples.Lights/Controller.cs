using System.Text.Json;

using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace Samples.Lights;

[TeamsController]
public class Controller
{
    private readonly OpenAIChatPrompt _prompt;

    public Controller(OpenAIChatPrompt prompt)
    {
        _prompt = prompt;
    }

    [Message("/history")]
    public async Task OnHistory(IContext<MessageActivity> context)
    {
        var state = State.From(context);
        await context.Send(JsonSerializer.Serialize(state.Messages, new JsonSerializerOptions()
        {
            WriteIndented = true
        }));
    }

    [Message]
    public async Task OnMessage(IContext<MessageActivity> context)
    {
        var state = State.From(context);

        await _prompt.Send(context.Activity.Text, new() { Messages = state.Messages }, (chunk) => Task.Run(() =>
        {
            context.Stream.Emit(chunk);
        }), context.CancellationToken);

        state.Save(context);
    }
}