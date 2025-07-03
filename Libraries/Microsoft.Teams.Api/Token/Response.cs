// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Token;

/// <summary>
/// A response that includes a user token
/// </summary>
public class Response
{
    /// <summary>
    /// the channel id
    /// </summary>
    [JsonPropertyName("channelId")]
    [JsonPropertyOrder(0)]
    public ChannelId? ChannelId { get; set; }

    /// <summary>
    /// The connection name
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(1)]
    public required string ConnectionName { get; set; }

    /// <summary>
    /// The user token
    /// </summary>
    [JsonPropertyName("token")]
    [JsonPropertyOrder(2)]
    public required string Token { get; set; }

    /// <summary>
    /// Expiration for the token, in ISO 8601 format (e.g. "2007-04-05T14:30Z")
    /// </summary>
    [JsonPropertyName("expiration")]
    [JsonPropertyOrder(3)]
    public string? Expiration { get; set; }

    /// <summary>
    /// A collection of properties about this response, such as token polling parameters
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonPropertyOrder(4)]
    public IDictionary<string, object?>? Properties { get; set; }
}