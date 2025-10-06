using System.Text.Json;

using Json.More;

using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.Entities.IEntity ToTeamsEntity(this Types.Entity entity)
    {
        var data = entity.Properties;
        data["type"] = entity.Type.AsJsonElement();

        return JsonSerializer.Deserialize<Api.Entities.IEntity>(JsonSerializer.Serialize(data))
            ?? throw new InvalidDataException();
    }
}

public static partial class AgentExtensions
{
    public static Types.Entity ToAgentEntity(this Api.Entities.IEntity entity)
    {
        return JsonSerializer.Deserialize<Types.Entity>(JsonSerializer.Serialize(entity))
            ?? throw new InvalidDataException();
    }
}