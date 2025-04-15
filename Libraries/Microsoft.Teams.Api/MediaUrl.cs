using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// Media URL
/// </summary>
public class MediaUrl
{
    /// <summary>
    /// URL for the media
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(0)]
    public required string Url { get; set; }

    /// <summary>
    /// Optional profile hint to the client to differentiate multiple MediaUrl objects from each other
    /// </summary>
    [JsonPropertyName("profile")]
    [JsonPropertyOrder(1)]
    public string? Profile { get; set; }
}