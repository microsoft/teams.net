using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.SignIn;

/// <summary>
/// SignIn ExchangeToken
/// </summary>
public class ExchangeToken
{
    /// <summary>
    /// the token id
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// the token
    /// </summary>
    [JsonPropertyName("token")]
    [JsonPropertyOrder(1)]
    public string? Token { get; set; }

    /// <summary>
    /// the connection name
    /// </summary>
    [JsonPropertyName("connectionName")]
    [JsonPropertyOrder(2)]
    public required string ConnectionName { get; set; }
}