// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
public class TeamsChannelData
{
    /// <summary>
    /// Creates a new instance of the <see cref="TeamsChannelData"/> class.
    /// </summary>
    public TeamsChannelData()
    {
    }

    /// <summary>
    /// Gets or sets the collection of additional properties not explicitly defined by the type.
    /// </summary>
    /// <remarks>This property stores extra JSON fields encountered during deserialization that do not map to
    /// known properties. It enables round-tripping of unknown or custom data without loss.</remarks>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

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

    /// <summary>
    /// Feedback loop configuration. When set, takes precedence over
    /// <see cref="FeedbackLoopEnabled"/>. Set <c>Type</c> to
    /// <see cref="FeedbackType.Custom"/> to trigger a <c>message/fetchTask</c>
    /// invoke for a bot-provided task module dialog.
    /// </summary>
    [JsonPropertyName("feedbackLoop")] public FeedbackLoop? FeedbackLoop { get; set; }
}

/// <summary>
/// Known values for <see cref="FeedbackLoop.Type"/>.
/// </summary>
public static class FeedbackType
{
    /// <summary>Teams' built-in thumbs up/down UI.</summary>
    public const string Default = "default";

    /// <summary>
    /// Triggers a <c>message/fetchTask</c> invoke so the bot can return its
    /// own task module dialog when the user clicks thumbs up/down.
    /// </summary>
    public const string Custom = "custom";
}

/// <summary>
/// Configuration for a feedback loop on a message. Serializes to
/// <c>channelData.feedbackLoop</c>. Must not coexist with
/// <see cref="TeamsChannelData.FeedbackLoopEnabled"/> — Teams rejects activities
/// that set both.
/// </summary>
public class FeedbackLoop
{
    /// <summary>
    /// The feedback loop type. See <see cref="FeedbackType"/> for known values.
    /// </summary>
    [JsonPropertyName("type")] public string Type { get; set; } = FeedbackType.Default;

    /// <summary>
    /// Creates a new instance with the default <see cref="FeedbackType.Default"/> type.
    /// </summary>
    public FeedbackLoop() { }

    /// <summary>
    /// Creates a new instance with the specified type.
    /// </summary>
    /// <param name="type">The feedback loop type. See <see cref="FeedbackType"/> for known values.</param>
    public FeedbackLoop(string type) { Type = type; }
}
