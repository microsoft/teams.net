// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

public class StreamInfoEntity : Entity
{
    [JsonPropertyName("streamId")]
    [JsonPropertyOrder(3)]
    public string? StreamId { get; set; }

    [JsonPropertyName("streamType")]
    [JsonPropertyOrder(4)]
    public StreamType? StreamType { get; set; }

    [JsonPropertyName("streamSequence")]
    [JsonPropertyOrder(5)]
    public int? StreamSequence { get; set; }

    public StreamInfoEntity() : base("streaminfo") { }
}

[JsonConverter(typeof(JsonConverter<StreamType>))]
public class StreamType(string value) : Common.StringEnum(value)
{
    public static readonly StreamType Informative = new("informative");
    public bool IsInformative => Informative.Equals(Value);

    public static readonly StreamType Streaming = new("streaming");
    public bool IsStreaming => Streaming.Equals(Value);

    public static readonly StreamType Final = new("final");
    public bool IsFinal => Final.Equals(Value);
}