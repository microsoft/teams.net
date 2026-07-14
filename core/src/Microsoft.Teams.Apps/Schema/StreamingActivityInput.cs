// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents an outbound streaming activity chunk. Has type "typing" to satisfy the Teams
/// streaming API, but carries text content that accumulates into the final response.
/// </summary>
public class StreamingActivityInput : TeamsActivityInput
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    internal StreamingActivityInput() : base(TeamsActivityTypes.Typing)
    {
    }

    /// <summary>
    /// Gets or sets the text content of the streaming chunk.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the stream info entity for this streaming activity.
    /// </summary>
    [JsonIgnore]
    public StreamInfoEntity? StreamInfo { get; set; }

    /// <summary>
    /// Serializes the current activity to a JSON string using the outbound streaming serializer context.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public override string ToJson()
        => JsonSerializer.Serialize(this, TeamsActivityInputJsonContext.Default.StreamingActivityInput);

    /// <summary>
    /// Creates a new <see cref="StreamingActivityInputBuilder"/> to construct an outbound streaming activity.
    /// </summary>
    /// <returns>A new <see cref="StreamingActivityInputBuilder"/> instance.</returns>
    public static new StreamingActivityInputBuilder CreateBuilder() => new();
}
