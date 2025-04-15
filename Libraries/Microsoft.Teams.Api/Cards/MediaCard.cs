using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Cards;

/// <summary>
/// Media Card
/// </summary>
public class MediaCard : Card
{
    /// <summary>
    /// Thumbnail placeholder
    /// </summary>
    [JsonPropertyName("image")]
    [JsonPropertyOrder(4)]
    public ThumbnailUrl? Image { get; set; }

    /// <summary>
    /// Media URLs for this card. When this field contains more than one URL, each URL is an
    /// alternative format of the same content.
    /// </summary>
    [JsonPropertyName("media")]
    [JsonPropertyOrder(5)]
    public IList<MediaUrl>? Media { get; set; }

    /// <summary>
    /// This content may be shared with others (default:true)
    /// </summary>
    [JsonPropertyName("shareable")]
    [JsonPropertyOrder(6)]
    public bool? Shareable { get; set; }

    /// <summary>
    /// Should the client loop playback at end of content (default:true)
    /// </summary>
    [JsonPropertyName("autoloop")]
    [JsonPropertyOrder(7)]
    public bool? AutoLoop { get; set; }

    /// <summary>
    /// Should the client automatically start playback of media in this card (default:true)
    /// </summary>
    [JsonPropertyName("autostart")]
    [JsonPropertyOrder(8)]
    public bool? AutoStart { get; set; }

    /// <summary>
    /// Aspect ratio of thumbnail/media placeholder. Allowed values are "16:9" and "4:3"
    /// </summary>
    [JsonPropertyName("aspect")]
    [JsonPropertyOrder(9)]
    public AspectRatio? Aspect { get; set; }

    /// <summary>
    /// Describes the length of the media content without requiring a receiver to open the content.
    /// Formatted as an ISO 8601 Duration field.
    /// </summary>
    [JsonPropertyName("duration")]
    [JsonPropertyOrder(10)]
    public string? Duration { get; set; }

    /// <summary>
    /// Supplementary parameter for this card
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(11)]
    public object? Value { get; set; }
}