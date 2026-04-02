using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace Samples.McpClient;

[Obsolete]
[TeamsController]
public class Controller(Func<OpenAIChatPrompt> _promptFactory)
{
    [Message]
    public async Task OnMessage(IContext<MessageActivity> context)
    {
        var prompt = _promptFactory();
        await prompt.Send(context.Activity.Text, new(), (chunk) => Task.Run(() =>
        {
            context.Stream.Emit(chunk);
        }), context.CancellationToken);
    }
}