// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents a streaming activity chunk. Has type "typing" to satisfy the Teams
/// streaming API, but carries text content that accumulates into the final response.
/// Construct via <see cref="CreateBuilder"/>.
/// </summary>
public class StreamingActivity : TeamsActivity
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    internal StreamingActivity() : base(TeamsActivityTypes.Typing)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivity"/> class with the specified text
    /// and a default streaming <see cref="StreamInfoEntity"/>.
    /// </summary>
    /// <param name="text">The accumulated text content of the streaming chunk.</param>
    internal StreamingActivity(string text) : base(TeamsActivityTypes.Typing)
    {
        Text = text;
        StreamInfo = StreamInfoEntityExtensions.AddToActivity(this, StreamTypes.Streaming);
    }

    /// <summary>
    /// Gets the text content of the streaming chunk.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; internal set; }

    /// <summary>
    /// Gets the stream info entity for this streaming activity.
    /// </summary>
    public StreamInfoEntity? StreamInfo { get; internal set; }

    /// <summary>
    /// Creates a new <see cref="StreamingActivityBuilder"/> to construct a streaming activity.
    /// </summary>
    /// <returns>A new <see cref="StreamingActivityBuilder"/> instance.</returns>
    public static new StreamingActivityBuilder CreateBuilder() => new();
}

/// <summary>
/// Provides a fluent API for building <see cref="StreamingActivity"/> instances.
/// This is the only supported way to construct a <see cref="StreamingActivity"/>.
/// </summary>
public class StreamingActivityBuilder : TeamsActivityBuilder<StreamingActivity, StreamingActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivityBuilder"/> class.
    /// </summary>
    internal StreamingActivityBuilder() : base(new StreamingActivity())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivityBuilder"/> class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal StreamingActivityBuilder(StreamingActivity activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the accumulated text content of the streaming chunk.
    /// </summary>
    public StreamingActivityBuilder WithText(string text)
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
    public StreamingActivityBuilder WithStreamInfo(string streamType, string? streamId = null, int? streamSequence = null)
    {
        _activity.StreamInfo = StreamInfoEntityExtensions.AddToActivity(_activity, streamType, streamId, streamSequence);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="StreamingActivity"/> instance.
    /// </summary>
    public override StreamingActivity Build() => _activity;
}
