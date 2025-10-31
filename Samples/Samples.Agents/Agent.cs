using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;

namespace Samples.Agents;

public class Agent : AgentApplication
{
    public Agent(Microsoft.Teams.Plugins.Agents.TeamsAgentPlugin plugin, AgentApplicationOptions options) : base(options)
    {
        RegisterExtension(plugin, plugin =>
        {
            plugin.AddRoute(
                this,
                (context, cancellationToken) => Task.FromResult(true),
                plugin.Do,
                false,
                RouteRank.First,
                []
            );
        });

        OnMessage((ctx, state) => Task.FromResult(true), OnMessage);
        OnMessageReactionsAdded(OnMessageReactionAdded);
    }

    public async Task OnMessage(ITurnContext context, ITurnState state, CancellationToken cancellationToken)
    {
        await context.SendActivityAsync($"Agent Application => you said '{context.Activity.Text}'", cancellationToken: cancellationToken);
    }

    public async Task OnMessageReactionAdded(ITurnContext context, ITurnState state, CancellationToken cancellationToken)
    {
        await context.SendActivityAsync($"Agent Application => you reacted with '{context.Activity.ReactionsAdded[0].Type}'", cancellationToken: cancellationToken);
    }
}