// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TokenExchange;

/// <summary>
/// An interface representing TokenExchangeRequest.
/// </summary>
public class Request
{
    /// <summary>
    /// the request uri
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonPropertyOrder(0)]
    public string? Uri { get; set; }

    /// <summary>
    /// the request token
    /// </summary>
    [JsonPropertyName("token")]
    [JsonPropertyOrder(1)]
    public string? Token { get; set; }
}