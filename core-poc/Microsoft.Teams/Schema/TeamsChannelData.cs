using Microsoft.Bot.Core.Schema;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Schema;

public class TeamsChannelData : ChannelData
{
    public TeamsChannelData()
    {
    }

    public TeamsChannelData(ChannelData cd)
    {
        if (cd is not null)
        {
            if (cd.Properties.TryGetValue("teamsChannelId", out object? channelIdObj) && channelIdObj is JsonElement jeChannelId && jeChannelId.ValueKind == JsonValueKind.String)
            {
                TeamsChannelId = jeChannelId.GetString();
            }

            if (cd.Properties.TryGetValue("teamsChannelId", out object? teamsChannelId) && teamsChannelId is JsonElement teamsChannelIdJE && teamsChannelIdJE.ValueKind == JsonValueKind.String)
            {
                TeamsChannelId = teamsChannelIdJE.GetString();
            }

            if (cd.Properties.TryGetValue("channel", out object? channelObj) && channelObj is JsonElement channelObjJE && channelObjJE.ValueKind == JsonValueKind.Object)
            {
                Channel = JsonSerializer.Deserialize<TeamsChannel?>(channelObjJE.GetRawText());
            }

            if (cd.Properties.TryGetValue("tenant", out object? tenantObj) && tenantObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                Tenant = JsonSerializer.Deserialize<TeamsChannelDataTenant>(je.GetRawText())!;
            }
        }
    }


    [JsonPropertyName("settings")]
    public TeamsChannelDataSettings? Settings { get; set; }

    [JsonPropertyName("teamsChannelId")]
    public string? TeamsChannelId { get; set; }

    [JsonPropertyName("teamsTeamId")]
    public string? TeamsTeamId { get; set; }

    [JsonPropertyName("channel")]
    public TeamsChannel? Channel { get; set; }

    [JsonPropertyName("team")]
    public Team? Team { get; set; }

    [JsonPropertyName("tenant")]
    public TeamsChannelDataTenant? Tenant { get; set; }

}
