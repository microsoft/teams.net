using System.Text.Json;

using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.Account ToTeamsEntity(this Types.ChannelAccount account)
    {
        return new()
        {
            Id = account.Id,
            AadObjectId = account.AadObjectId,
            Name = account.Name,
            Role = new(account.Role),
            Properties = account.Properties.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Deserialize<object>()
            ),
        };
    }
}

public static partial class AgentExtensions
{
    public static Types.ChannelAccount ToAgentEntity(this Api.Account account)
    {
        return new()
        {
            Id = account.Id,
            AadObjectId = account.AadObjectId,
            Name = account.Name,
            Role = account.Role?.ToString(),
            Properties = account.Properties?.ToDictionary(
                pair => pair.Key,
                pair => JsonSerializer.SerializeToElement(pair.Value)
            ),
        };
    }
}