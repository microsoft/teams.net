using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Agents.Core.Serialization;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static Api.ChannelData ToTeamsChannelData(this object value)
    {
        var type = typeof(Api.ChannelData);
        var channelData = new Api.ChannelData();
        var json = value.ToJsonElements();

        foreach (var field in type.GetFields())
        {
            var attribute = field.GetCustomAttribute<JsonPropertyNameAttribute>();

            if (attribute is null)
            {
                continue;
            }

            if (json.TryGetValue(attribute.Name, out var el))
            {
                field.SetValue(channelData, el.Deserialize(field.FieldType));
            }
        }

        return channelData;
    }
}

public static partial class AgentExtensions
{
    public static object ToAgentChannelData(this Api.ChannelData value)
    {
        return value.ToJsonElements();
    }
}