using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// An app info object that describes an app
/// </summary>
public class App
{
    /// <summary>
    /// Unique identifier representing an app
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// All extra data present
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}