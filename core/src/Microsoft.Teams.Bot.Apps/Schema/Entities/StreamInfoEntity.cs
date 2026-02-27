// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

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
