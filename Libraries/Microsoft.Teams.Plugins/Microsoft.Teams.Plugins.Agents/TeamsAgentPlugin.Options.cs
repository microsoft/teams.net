using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Teams.Plugins.Agents;

public class TeamsAgentPluginOptions
{
    public IServiceProvider? Provider { get; set; }
    public RoutingStrategy RoutingStrategy { get; set; } = RoutingStrategy.Broadcast;
    public RoutingHandler? RoutingFactory { get; set; } = null;

    internal ITurnContext.Accessor ContextAccessor => Provider?.GetRequiredService<ITurnContext.Accessor>() ?? throw new Exception("ITurnContext.Accessor not found");
    internal Microsoft.Agents.Builder.ITurnContext Context => ContextAccessor.Value ?? throw new Exception("ITurnContext not found");
}

public enum RoutingStrategy
{
    /// <summary>
    /// route to both teams and agents applications
    /// </summary>
    Broadcast,

    /// <summary>
    /// route to the agents application
    /// </summary>
    Agents,

    /// <summary>
    /// route to the teams application
    /// </summary>
    Teams
}

public delegate Task<RoutingStrategy> RoutingHandler(Microsoft.Agents.Builder.ITurnContext context, CancellationToken cancellationToken = default);