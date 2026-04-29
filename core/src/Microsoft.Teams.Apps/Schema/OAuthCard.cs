// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Core;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Represents an OAuthCard used to initiate an OAuth sign-in flow.
/// </summary>
public class OAuthCard
{
    /// <summary>
    /// The text displayed on the card.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// The OAuth connection name configured on the bot.
    /// </summary>
    [JsonPropertyName("connectionName")]
    public string? ConnectionName { get; set; }

    /// <summary>
    /// The sign-in action buttons.
    /// </summary>
    [JsonPropertyName("buttons")]
    public IList<SuggestedAction>? Buttons { get; set; }

    /// <summary>
    /// The token exchange resource for SSO.
    /// When present, the Teams client attempts a silent token exchange before showing the sign-in button.
    /// </summary>
    [JsonPropertyName("tokenExchangeResource")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenExchangeResource? TokenExchangeResource { get; set; }

    /// <summary>
    /// The token post resource for posting the token back after sign-in.
    /// </summary>
    [JsonPropertyName("tokenPostResource")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenPostResource? TokenPostResource { get; set; }
}
