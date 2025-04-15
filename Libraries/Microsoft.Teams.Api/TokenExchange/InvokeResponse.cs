using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TokenExchange;

/// <summary>
/// The response object of a token exchange invoke.
/// </summary>
public class InvokeResponse
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
    /// The details of why the token exchange failed.
    /// </summary>
    [JsonPropertyName("failureDetail")]
    [JsonPropertyOrder(2)]
    public string? FailureDetail { get; set; }

    /// <summary>
    /// Extension data for overflow of properties.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Properties = new Dictionary<string, object?>();
}