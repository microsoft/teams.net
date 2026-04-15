// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Extension methods for activity stream info.
/// </summary>
public static class ActivityStreamInfoExtensions
{
    /// <summary>
    /// Adds a stream info entity to the activity.
    /// </summary>
    /// <param name="activity">The activity to add stream info to. Cannot be null.</param>
    /// <param name="streamType">The stream type. See <see cref="StreamType"/> for possible values.</param>
    /// <param name="streamId">Optional stream identifier.</param>
    /// <param name="streamSequence">Optional stream sequence number.</param>
    /// <returns>The created StreamInfoEntity that was added to the activity.</returns>
    public static StreamInfoEntity AddStreamInfo(this TeamsActivity activity, string streamType, string? streamId = null, int? streamSequence = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        StreamInfoEntity streamInfo = new()
        {
            StreamType = streamType,
            StreamId = streamId,
            StreamSequence = streamSequence
        };
        activity.Entities ??= [];
        activity.Entities.Add(streamInfo);
        activity.Rebase();
        return streamInfo;
    }

    /// <summary>
    /// Gets the stream info entity from the activity's entity collection, if present.
    /// </summary>
    /// <param name="activity">The activity to read from. Cannot be null.</param>
    /// <returns>The StreamInfoEntity if found; otherwise, null.</returns>
    public static StreamInfoEntity? GetStreamInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity.Entities?.FirstOrDefault(e => e is StreamInfoEntity) as StreamInfoEntity;
    }
}

/// <summary>
/// Stream info entity.
/// </summary>
public class StreamInfoEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="StreamInfoEntity"/>.
    /// </summary>
    public StreamInfoEntity() : base("streaminfo") { }

    /// <summary>
    /// Gets or sets the stream id.
    /// </summary>
    [JsonPropertyName("streamId")]
    public string? StreamId
    {
        get => base.Properties.TryGetValue("streamId", out object? value) ? value?.ToString() : null;
        set => base.Properties["streamId"] = value;
    }

    /// <summary>
    /// Gets or sets the stream type. See <see cref="StreamType"/> for possible values.
    /// </summary>
    [JsonPropertyName("streamType")]
    public string? StreamType
    {
        get => base.Properties.TryGetValue("streamType", out object? value) ? value?.ToString() : null;
        set => base.Properties["streamType"] = value;
    }

    /// <summary>
    /// Gets or sets the stream sequence.
    /// </summary>
    [JsonPropertyName("streamSequence")]
    public int? StreamSequence
    {
        get => base.Properties.TryGetValue("streamSequence", out object? value) && value != null
            ? (int.TryParse(value.ToString(), out int intVal) ? intVal : null)
            : null;
        set => base.Properties["streamSequence"] = value;
    }
}

/// <summary>
/// Represents the types of streams.
/// </summary>
public static class StreamType
{
    /// <summary>
    /// Informative stream type.
    /// </summary>
    public const string Informative = "informative";
    /// <summary>
    /// Streaming stream type.
    /// </summary>
    public const string Streaming = "streaming";
    /// <summary>
    /// Represents the string literal "final".
    /// </summary>
    public const string Final = "final";
}
