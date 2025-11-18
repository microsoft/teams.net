// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Auth;

public interface ITokenResponse
{
    public string TokenType { get; }
    public int? ExpiresIn { get; }
    public string AccessToken { get; }
}

public class TokenResponse : ITokenResponse
{
    [JsonPropertyName("token_type")]
    [JsonPropertyOrder(0)]
    public required string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    [JsonPropertyOrder(1)]
    public int? ExpiresIn { get; }

    [JsonPropertyName("access_token")]
    [JsonPropertyOrder(2)]
    public required string AccessToken { get; set; }
}