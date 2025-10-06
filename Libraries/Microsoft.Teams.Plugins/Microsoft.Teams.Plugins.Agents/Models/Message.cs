using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.Messages.Reaction ToTeamsEntity(this Types.MessageReaction reaction)
    {
        return new()
        {
            Type = new(reaction.Type)
        };
    }
}

public static partial class AgentExtensions
{
    public static Types.MessageReaction ToAgentEntity(this Api.Messages.Reaction reaction)
    {
        return new()
        {
            Type = reaction.Type
        };
    }
}