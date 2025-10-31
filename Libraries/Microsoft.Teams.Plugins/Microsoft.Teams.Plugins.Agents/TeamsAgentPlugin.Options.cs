using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Teams.Plugins.Agents;

public class TeamsAgentPluginOptions
{
    public IServiceProvider? Provider { get; set; }

    internal ITurnContext.Accessor ContextAccessor => Provider?.GetRequiredService<ITurnContext.Accessor>() ?? throw new Exception("ITurnContext.Accessor not found");
    internal Microsoft.Agents.Builder.ITurnContext Context => ContextAccessor.Value ?? throw new Exception("ITurnContext not found");
}
