// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Auth;

/// <summary>
/// Response body returned in the invoke response for a failed signin/tokenExchange.
/// Sent with HTTP 412 (PreconditionFailed) to tell Teams to fall back to the sign-in card.
/// </summary>
public class TokenExchangeInvokeResponse
{
    /// <summary>
    /// The token exchange request ID (echoed from the invoke value).
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The OAuth connection name (echoed from the invoke value).
    /// </summary>
    [JsonPropertyName("connectionName")]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Details about why the token exchange failed.
    /// </summary>
    [JsonPropertyName("failureDetail")]
    public string? FailureDetail { get; set; }
}
