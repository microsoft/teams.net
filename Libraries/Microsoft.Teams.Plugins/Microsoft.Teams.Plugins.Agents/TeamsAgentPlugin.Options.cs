using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Teams.Plugins.Agents;

public class TeamsAgentPluginOptions
{
    public IServiceProvider? Provider { get; set; }

    internal TurnContextAccessor ContextAccessor => Provider?.GetRequiredService<TurnContextAccessor>() ?? throw new Exception("TurnContextAccessor not found");
    internal Microsoft.Agents.Builder.ITurnContext Context => ContextAccessor.Value ?? throw new Exception("ITurnContext not found");
}
