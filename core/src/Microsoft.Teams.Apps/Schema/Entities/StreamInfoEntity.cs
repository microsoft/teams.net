// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Schema.Entities;

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
    /// Gets or sets the stream type. See <see cref="StreamTypes"/> for possible values.
    /// </summary>
    [JsonPropertyName("streamType")]
    public StreamType? StreamType
    {
        get => base.Properties.TryGetValue("streamType", out object? value) && value is not null ? new StreamType(value.ToString()!) : null;
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
/// String enum for stream types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<StreamType>))]
public class StreamType(string value) : StringEnum(value)
{
    /// <summary>Gets the informative stream type.</summary>
    public static readonly StreamType Informative = new("informative");
    /// <summary>Gets the streaming stream type.</summary>
    public static readonly StreamType Streaming = new("streaming");
    /// <summary>Gets the final stream type.</summary>
    public static readonly StreamType Final = new("final");

}

/// <summary>
/// Common stream type values.
/// </summary>
public static class StreamTypes
{
    /// <summary>Gets the informative stream type.</summary>
    public static StreamType Informative => StreamType.Informative;

    /// <summary>Gets the streaming stream type.</summary>
    public static StreamType Streaming => StreamType.Streaming;

    /// <summary>Gets the final stream type.</summary>
    public static StreamType Final => StreamType.Final;
}
