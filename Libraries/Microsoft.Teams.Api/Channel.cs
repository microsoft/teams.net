// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// A channel info object which decribes the channel.
/// </summary>
public class Channel
{
    /// <summary>
    /// Unique identifier representing a channel
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The type of the channel. Valid values are standard, shared and private
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public ChannelType? Type { get; set; }

    /// <summary>
    /// Name of the channel
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(2)]
    public string? Name { get; set; }
}

[JsonConverter(typeof(JsonConverter<ChannelType>))]
public class ChannelType(string value) : Common.StringEnum(value)
{
    public static readonly ChannelType Standard = new("standard");
    public bool IsStandard => Standard.Equals(Value);

    public static readonly ChannelType Shared = new("shared");
    public bool IsShared => Shared.Equals(Value);

    public static readonly ChannelType Private = new("private");
    public bool IsPrivate => Private.Equals(Value);
}