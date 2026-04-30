// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.OAuth;

/// <summary>
/// Value payload of the signin/tokenExchange invoke activity.
/// </summary>
public class SignInTokenExchangeValue
{
    /// <summary>
    /// Unique identifier for this token exchange request, used for deduplication.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The OAuth connection name this exchange targets.
    /// </summary>
    [JsonPropertyName("connectionName")]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// The token provided by the Teams client for exchange.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}
