using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Plugins.Agents.Extensions;

namespace Samples.Agents;

[TeamsController]
public class Controller
{
    [Message]
    public async Task OnMessage(IContext<MessageActivity> context, [Context] MessageActivity activity, [Context] IContext.Client client)
    {
        await context.GetTurnContext().SendActivityAsync("Teams Application => hi from the turn context!");
        await client.Send($"Teams Application => you said '{activity.Text}'");
    }
}