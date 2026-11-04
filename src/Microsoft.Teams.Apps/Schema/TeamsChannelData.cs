// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;
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
/// Meeting information for Teams channel data, present on activities in a meeting chat.
/// </summary>
public class TeamsChannelDataMeeting
{
    /// <summary>
    /// Unique identifier of the meeting.
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
    [JsonPropertyName("selectedChannel")] public TeamsChannel? SelectedChannel { get; set; }

    /// <summary>
    /// Gets or sets the collection of additional properties not explicitly defined by the type.
    /// </summary>
    /// <remarks>This property stores extra JSON fields encountered during deserialization that do not map to
    /// known properties. It enables round-tripping of unknown or custom data without loss. The dictionary keys
    /// correspond to the property names in the JSON payload.</remarks>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];
}

/// <summary>
/// Information about the app sending an activity.
/// </summary>
public class AppInfo
{
    /// <summary>
    /// Unique identifier representing an app.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Version of the app.
    /// </summary>
    [JsonPropertyName("version")] public string? Version { get; set; }
}

/// <summary>
/// The encrypted form of the id of a chat scope.
/// </summary>
/// <remarks>
/// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
/// </remarks>
public sealed class EncryptedChatInfo
{
    /// <summary>
    /// Creates encrypted chat information for deserialization.
    /// </summary>
    /// <param name="id">The encrypted id of the chat scope.</param>
    [JsonConstructor]
    public EncryptedChatInfo(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the encrypted id of the chat scope.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; }
}

/// <summary>
/// Identifies the chat scope that contains an inbound activity.
/// </summary>
/// <remarks>
/// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
/// </remarks>
public sealed class ChatInfo
{
    /// <summary>
    /// Creates chat information for deserialization.
    /// </summary>
    /// <param name="id">The id of the chat scope.</param>
    /// <param name="encrypted">The encrypted form of the chat scope's id.</param>
    [JsonConstructor]
    public ChatInfo(string? id, EncryptedChatInfo? encrypted)
    {
        Id = id;
        Encrypted = encrypted;
    }

    /// <summary>
    /// Gets the id of the chat scope, without any reply chain (thread).
    /// This is the unencrypted id, and it can differ from the conversation id.
    /// It is not sent for channels, where <c>channelData.channel.id</c> plays this role.
    /// Use it as the conversation id to send a top-level message to the chat scope, or to reply in a reply chain.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; }

    /// <summary>
    /// Gets the encrypted form of the chat scope's id.
    /// </summary>
    [JsonPropertyName("encrypted")]
    public EncryptedChatInfo? Encrypted { get; }
}

/// <summary>
/// Identifies the reply chain (thread) that an inbound activity belongs to, by the message at the root of the chain.
/// </summary>
/// <remarks>
/// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
/// </remarks>
public sealed class ReplyChainInfo
{
    /// <summary>
    /// Creates reply chain information for deserialization.
    /// </summary>
    /// <param name="id">The id of the message at the root of the reply chain (thread).</param>
    [JsonConstructor]
    public ReplyChainInfo(string? id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the id of the message at the root of the reply chain (thread).
    /// Use it as the reply chain (thread) id when replying in that reply chain (thread).
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; }
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
    /// Information about the app sending this activity.
    /// </summary>
    [JsonPropertyName("app")] public AppInfo? App { get; set; }

    /// <summary>
    /// Gets information about the chat scope that contains the activity.
    /// </summary>
    /// <remarks>
    /// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
    /// </remarks>
    [JsonPropertyName("chat")]
    [JsonInclude]
    public ChatInfo? Chat { get; internal set; }

    /// <summary>
    /// Gets the reply chain (thread) the activity belongs to, identified by its root message.
    /// </summary>
    /// <remarks>
    /// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
    /// </remarks>
    [JsonPropertyName("replyChain")]
    [JsonInclude]
    public ReplyChainInfo? ReplyChain { get; internal set; }

    /// <summary>
    /// Team information.
    /// </summary>
    [JsonPropertyName("team")] public Team? Team { get; set; }

    /// <summary>
    /// Information about the meeting whose chat contains the activity.
    /// </summary>
    [JsonPropertyName("meeting")] public TeamsChannelDataMeeting? Meeting { get; set; }

    /// <summary>
    /// Tenant information.
    /// </summary>
    [JsonPropertyName("tenant")] public TeamsChannelDataTenant? Tenant { get; set; }

    /// <summary>
    /// Gets or sets the event type for conversation updates. See <see cref="ConversationEventTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("eventType")] public ConversationEventType? EventType { get; set; }

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
    /// <see cref="FeedbackTypes.Custom"/> to trigger a <c>message/fetchTask</c>
    /// invoke for a bot-provided task module dialog.
    /// </summary>
    [JsonPropertyName("feedbackLoop")] public FeedbackLoop? FeedbackLoop { get; set; }
}

/// <summary>
/// String enum for <see cref="FeedbackLoop.Type"/>.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<FeedbackType>))]
public class FeedbackType(string value) : StringEnum(value)
{
    /// <summary>Gets the default feedback loop type.</summary>
    public static readonly FeedbackType Default = new("default");
    /// <summary>Gets the custom feedback loop type.</summary>
    public static readonly FeedbackType Custom = new("custom");

}

/// <summary>
/// Common feedback loop types.
/// </summary>
public static class FeedbackTypes
{
    /// <summary>Gets the default feedback loop type.</summary>
    public static FeedbackType Default => FeedbackType.Default;

    /// <summary>Gets the custom feedback loop type.</summary>
    public static FeedbackType Custom => FeedbackType.Custom;
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
    /// The feedback loop type. See <see cref="FeedbackTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("type")] public FeedbackType Type { get; set; } = FeedbackTypes.Default;

    /// <summary>
    /// Creates a new instance with the default <see cref="FeedbackTypes.Default"/> type.
    /// </summary>
    public FeedbackLoop() { }

    /// <summary>
    /// Creates a new instance with the specified type.
    /// </summary>
    /// <param name="type">The feedback loop type. See <see cref="FeedbackTypes"/> for known values.</param>
    public FeedbackLoop(FeedbackType type) { Type = type; }
}
