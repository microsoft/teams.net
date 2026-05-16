// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents the source of a Teams activity.
/// </summary>
public class TeamsChannelDataSource
{
    /// <summary>
    /// The name of the source.
    /// </summary>
    [JsonPropertyName("name")] public string? Name { get; set; }
}

/// <summary>
/// Tenant information for Teams channel data.
/// </summary>
public class TeamsChannelDataTenant
{
    /// <summary>
    /// Unique identifier of the tenant.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }
}

/// <summary>
/// Teams channel data settings.
/// </summary>
public class TeamsChannelDataSettings
{
    /// <summary>
    /// Selected channel information.
    /// </summary>
    [JsonPropertyName("selectedChannel")] public required TeamsChannel SelectedChannel { get; set; }

    /// <summary>
    /// Gets or sets the collection of additional properties not explicitly defined by the type.
    /// </summary>
    /// <remarks>This property stores extra JSON fields encountered during deserialization that do not map to
    /// known properties. It enables round-tripping of unknown or custom data without loss. The dictionary keys
    /// correspond to the property names in the JSON payload.</remarks>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];
}

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

    /// <summary>
    /// Gets or sets the event type for conversation updates. See <see cref="ConversationEventTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("eventType")] public string? EventType { get; set; }

    /// <summary>
    /// Source information for the activity.
    /// </summary>
    [JsonPropertyName("source")] public TeamsChannelDataSource? Source { get; set; }

    /// <summary>
    /// Gets or sets whether the feedback loop (thumbs up/down) is enabled for the activity.
    /// </summary>
    [JsonPropertyName("feedbackLoopEnabled")] public bool? FeedbackLoopEnabled { get; set; }

}
