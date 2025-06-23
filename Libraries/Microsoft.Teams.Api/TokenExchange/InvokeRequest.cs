// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TokenExchange;

/// <summary>
/// A request to exchange a token.
/// </summary>
public class InvokeRequest
{
    /// <summary>
    /// The id from the OAuthCard.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The connection name.
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(1)]
    public required string ConnectionName { get; set; }

    /// <summary>
    /// The user token that can be exchanged.
    /// </summary>
    [JsonPropertyName("token")]
    [JsonPropertyOrder(2)]
    public required string Token { get; set; }
}