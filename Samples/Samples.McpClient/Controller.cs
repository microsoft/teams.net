using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;

namespace Samples.McpClient;

[TeamsController]
public class Controller(IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    [Message]
    public async Task OnMessage(IContext<MessageActivity> context)
    {
        var httpContext = _httpContextAccessor.HttpContext
                  ?? throw new InvalidOperationException("No active HttpContext. Cannot resolve OpenAIChatPrompt.");

        var prompt = httpContext.RequestServices.GetRequiredService<OpenAIChatPrompt>();
        await prompt.Send(context.Activity.Text, new(), (chunk) => Task.Run(() =>
        {
            context.Stream.Emit(chunk);
        }), context.CancellationToken);
    }
}