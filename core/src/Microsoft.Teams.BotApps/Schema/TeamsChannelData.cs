// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema;

/// <summary>
/// Represents Teams-specific channel data.
/// </summary>
public class TeamsChannelData : ChannelData
{
    /// <summary>
    /// Creates a new instance of the <see cref="TeamsChannelData"/> class.
    /// </summary>
    public TeamsChannelData()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TeamsChannelData"/> class from the specified <see cref="ChannelData"/> object.
    /// </summary>
    /// <param name="cd"></param>
    public TeamsChannelData(ChannelData cd)
    {
        if (cd is not null)
        {
            if (cd.Properties.TryGetValue("teamsChannelId", out object? channelIdObj) && channelIdObj is JsonElement jeChannelId && jeChannelId.ValueKind == JsonValueKind.String)
            {
                TeamsChannelId = jeChannelId.GetString();
            }

            if (cd.Properties.TryGetValue("channel", out object? channelObj) && channelObj is JsonElement channelObjJE && channelObjJE.ValueKind == JsonValueKind.Object)
            {
                Channel = JsonSerializer.Deserialize<TeamsChannel?>(channelObjJE.GetRawText());
            }

            if (cd.Properties.TryGetValue("tenant", out object? tenantObj) && tenantObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                Tenant = JsonSerializer.Deserialize<TeamsChannelDataTenant>(je.GetRawText());
            }
        }
    }


    /// <summary>
    /// Settings for the Teams channel.
    /// </summary>
    [JsonPropertyName("settings")] public TeamsChannelDataSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the Microsoft Teams channel associated with this entity.
    /// </summary>
    [JsonPropertyName("teamsChannelId")] public string? TeamsChannelId { get; set; }

    /// <summary>
    /// Teams Team Id.
    /// </summary>
    [JsonPropertyName("teamsTeamId")] public string? TeamsTeamId { get; set; }

    /// <summary>
    /// Gets or sets the channel information associated with this entity.
    /// </summary>
    [JsonPropertyName("channel")] public TeamsChannel? Channel { get; set; }

    /// <summary>
    /// Team information.
    /// </summary>
    [JsonPropertyName("team")] public Team? Team { get; set; }

    /// <summary>
    /// Tenant information.
    /// </summary>
    [JsonPropertyName("tenant")] public TeamsChannelDataTenant? Tenant { get; set; }

}
