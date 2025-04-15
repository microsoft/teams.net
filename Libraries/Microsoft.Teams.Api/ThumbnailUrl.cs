using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// Thumbnail URL
/// </summary>
public class ThumbnailUrl
{
    /// <summary>
    /// URL pointing to the thumbnail to use for media content
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(0)]
    public required string Url { get; set; }

    /// <summary>
    /// HTML alt text to include on this thumbnail image
    /// </summary>
    [JsonPropertyName("alt")]
    [JsonPropertyOrder(1)]
    public required string Alt { get; set; }
}