// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Token;

/// <summary>
/// The status of a particular token.
/// </summary>
public class Status
{
    /// <summary>
    /// The channel ID.
    /// </summary>
    [JsonPropertyName("channelId")]
    [JsonPropertyOrder(0)]
    public required ChannelId ChannelId { get; set; }

    /// <summary>
    /// The connection name.
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(1)]
    public required string ConnectionName { get; set; }

    /// <summary>
    /// Boolean indicating if a token is stored for this ConnectionName.
    /// </summary>
    [JsonPropertyName("hasToken")]
    [JsonPropertyOrder(2)]
    public required bool HasToken { get; set; }

    /// <summary>
    /// The display name of the service provider for which this Token belongs to.
    /// </summary>
    [JsonPropertyName("serviceProviderDisplayName")]
    [JsonPropertyOrder(3)]
    public required string ServiceProviderDisplayName { get; set; }
}