using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Cards;

/// <summary>
/// A card representing a request to perform a sign in via OAuth
/// </summary>
public class OAuthCard : Card
{
    /// <summary>
    /// Text for signin request
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(2)]
    public new required string Text { get; set; }

    /// <summary>
    /// The name of the registered connection
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(4)]
    public required string ConnectionName { get; set; }

    /// <summary>
    /// The token exchange resource for single sign on
    /// </summary>
    [JsonPropertyName("tokenExchangeResource")]
    [JsonPropertyOrder(5)]
    public TokenExchange.Resource? TokenExchangeResource { get; set; }

    /// <summary>
    /// The token for directly post a token to token service
    /// </summary>
    [JsonPropertyName("tokenPostResource")]
    [JsonPropertyOrder(6)]
    public Token.PostResource? TokenPostResource { get; set; }
}