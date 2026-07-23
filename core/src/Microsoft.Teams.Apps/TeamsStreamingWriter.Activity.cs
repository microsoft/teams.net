// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Outbound streaming activity chunk used by <see cref="TeamsStreamingWriter"/>.
/// Has type "typing" to satisfy the Teams streaming API, but carries text content that
/// accumulates into the final response.
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

/// <summary>
/// Fluent builder for <see cref="StreamingActivityInput"/>.
/// </summary>
public class StreamingActivityInputBuilder : TeamsActivityInputBuilder<StreamingActivityInput, StreamingActivityInputBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivityInputBuilder"/> class.
    /// </summary>
    public StreamingActivityInputBuilder() : base(new StreamingActivityInput())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivityInputBuilder"/> class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    public StreamingActivityInputBuilder(StreamingActivityInput activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the accumulated text content of the streaming chunk.
    /// </summary>
    public StreamingActivityInputBuilder WithText(string text)
    {
        _activity.Text = text;
        return this;
    }

    /// <summary>
    /// Sets the stream metadata for this chunk (writes channel data and adds a <see cref="StreamInfoEntity"/>).
    /// </summary>
    /// <param name="streamType">The stream type. See <see cref="StreamTypes"/>.</param>
    /// <param name="streamId">Optional stream identifier.</param>
    /// <param name="streamSequence">Optional monotonically increasing sequence number.</param>
    public StreamingActivityInputBuilder WithStreamInfo(string streamType, string? streamId = null, int? streamSequence = null)
    {
        _activity.StreamInfo = StreamInfoEntityExtensions.AddToActivity(_activity, streamType, streamId, streamSequence);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="StreamingActivityInput"/> instance.
    /// </summary>
    public override StreamingActivityInput Build() => _activity;
}
