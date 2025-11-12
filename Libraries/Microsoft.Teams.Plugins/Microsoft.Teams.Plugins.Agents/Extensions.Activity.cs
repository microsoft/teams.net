namespace Microsoft.Teams.Plugins.Agents;

using System.Text.Json;

using Microsoft.Teams.Api.Activities;

using Agents = Microsoft.Agents.Core;

public static partial class Extensions
{
    public static IActivity ToTeams<TActivity>(this TActivity activity) where TActivity : Agents.Models.IActivity
    {
        var json = JsonSerializer.Serialize(activity, Agents.Serialization.ProtocolJsonSerializer.SerializationOptions);
        return JsonSerializer.Deserialize<IActivity>(json) ?? throw new JsonException("could not convert Agents.Core.IActivity to Teams.Api.IActivity");
    }

    public static Agents.Models.IActivity ToAgents<TActivity>(this TActivity activity) where TActivity : IActivity
    {
        var json = JsonSerializer.Serialize(activity, Agents.Serialization.ProtocolJsonSerializer.SerializationOptions);
        return JsonSerializer.Deserialize<Agents.Models.IActivity>(json) ?? throw new JsonException("could not convert Teams.Api.IActivity to Agents.Core.IActivity");
    }
}