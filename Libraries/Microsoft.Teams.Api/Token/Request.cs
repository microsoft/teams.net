using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Token;

/// <summary>
/// A request to receive a user token
/// </summary>
public class Request
{
    /// <summary>
    /// The provider to request a user token from
    /// </summary>
    [JsonPropertyName("provider")]
    [JsonPropertyOrder(0)]
    public required string Provider { get; set; }

    /// <summary>
    /// A collection of settings for the specific provider for this request
    /// </summary>
    [JsonPropertyName("settings")]
    [JsonPropertyOrder(1)]
    public IDictionary<string, object?> Settings { get; set; } = new Dictionary<string, object?>();
}