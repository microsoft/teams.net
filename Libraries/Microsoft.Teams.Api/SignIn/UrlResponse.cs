// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.SignIn;

/// <summary>
/// An interface representing SignInUrlResponse.
/// </summary>
public class UrlResponse
{
    /// <summary>
    /// An interface representing SignInUrlResponse.
    /// </summary>
    [JsonPropertyName("signInLink")]
    [JsonPropertyOrder(0)]
    public string? SignInLink { get; set; }

    /// <summary>
    /// The token exchange resource
    /// </summary>
    [JsonPropertyName("tokenExchangeResource")]
    [JsonPropertyOrder(1)]
    public TokenExchange.Resource? TokenExchangeResource { get; set; }

    /// <summary>
    /// The token post resource
    /// </summary>
    [JsonPropertyName("tokenPostResource")]
    [JsonPropertyOrder(2)]
    public Token.PostResource? TokenPostResource { get; set; }
}