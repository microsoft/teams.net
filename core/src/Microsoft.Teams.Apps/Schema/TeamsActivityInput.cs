// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Apps.Utils;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Base type for outbound (outgoing) Teams activities constructed by builders and sent by the API clients.
/// </summary>
/// <remarks>
/// This is the Teams-layer counterpart to <see cref="CoreActivityInput"/>. It is the serialized
/// outbound shape and carries only sender-supplied content (entities, channel data, suggested actions).
/// Transport routing (service url, conversation id) is supplied explicitly to the API clients.
/// </remarks>
public class TeamsActivityInput : CoreActivityInput
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    internal TeamsActivityInput() : base(TeamsActivityTypes.Message)
    {
    }

    /// <summary>
    /// Constructor with type parameter.
    /// </summary>
    /// <param name="type">The activity type.</param>
    internal TeamsActivityInput(string type) : base(type)
    {
    }

    /// <summary>
    /// Gets or sets the Teams-specific channel data associated with this outbound activity.
    /// </summary>
    [JsonPropertyName("channelData")]
    public TeamsOutboundChannelData? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets the entities associated with this activity.
    /// </summary>
    [JsonPropertyName("entities")]
    public EntityList? Entities { get; set; }

    /// <summary>
    /// Gets or sets the suggested actions for the message.
    /// </summary>
    [JsonPropertyName("suggestedActions")]
    public SuggestedActions? SuggestedActions { get; set; }

    /// <summary>
    /// Serializes the current activity to a JSON string using the outbound Teams serializer context.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public override string ToJson()
        => JsonSerializer.Serialize(this, TeamsActivityInputJsonContext.Default.TeamsActivityInput);
}

/// <summary>
/// Minimal Teams channel data for outbound (outgoing) activities. Unlike the full inbound
/// <see cref="TeamsChannelData"/>, this exposes only the fields a sender actually populates
/// (feedback loop configuration and streaming metadata); inbound-only routing fields such as
/// team, channel, and tenant are intentionally omitted.
/// </summary>
public class TeamsOutboundChannelData : ChannelData
{
    /// <summary>
    /// Gets or sets whether the feedback loop (thumbs up/down) is enabled for the activity.
    /// </summary>
    [JsonPropertyName("feedbackLoopEnabled")] public bool? FeedbackLoopEnabled { get; set; }

    /// <summary>
    /// Gets or sets the feedback loop configuration. When set, takes precedence over
    /// <see cref="FeedbackLoopEnabled"/>.
    /// </summary>
    [JsonPropertyName("feedbackLoop")] public FeedbackLoop? FeedbackLoop { get; set; }

    /// <summary>
    /// Gets or sets the stream identifier shared across a streamed message's chunks.
    /// </summary>
    [JsonPropertyName("streamId")] public string? StreamId { get; set; }

    /// <summary>
    /// Gets or sets the stream type. See <see cref="StreamTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("streamType")] public StreamType? StreamType { get; set; }

    /// <summary>
    /// Gets or sets the monotonically increasing stream sequence number.
    /// </summary>
    [JsonPropertyName("streamSequence")] public int? StreamSequence { get; set; }
}
