using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace Samples.McpClient;

[TeamsController]
public class Controller(OpenAIChatPrompt _prompt)
{
    [Message]
    public async Task OnMessage(IContext<MessageActivity> context)
    {
        await _prompt.Send(context.Activity.Text, new(), (chunk) => Task.Run(() =>
        {
            context.Stream.Emit(chunk);
        }), context.CancellationToken);
    }
}