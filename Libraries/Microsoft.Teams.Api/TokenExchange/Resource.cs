using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TokenExchange;

/// <summary>
/// Defines information required to enable on-behalf-of single sign-on user authentication. Maps to the TokenExchangeResource type defined by the Bot Framework (https://docs.microsoft.com/dotnet/api/microsoft.bot.schema.tokenexchangeresource)
/// </summary>
public class Resource
{
    /// <summary>
    /// The unique identified of this token exchange instance.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// An application ID or resource identifier with which to exchange a token on behalf of. This property is identity provider- and application-specific.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonPropertyOrder(1)]
    public required string Uri { get; set; }

    /// <summary>
    /// An identifier for the identity provider with which to attempt a token exchange.
    /// </summary>
    [JsonPropertyName("providerId")]
    [JsonPropertyOrder(2)]
    public required string ProviderId { get; set; }
}