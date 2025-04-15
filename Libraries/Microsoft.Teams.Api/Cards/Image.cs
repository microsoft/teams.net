using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Cards;

/// <summary>
/// An image on a card
/// </summary>
public class Image
{
    /// <summary>
    /// URL thumbnail image for major content property
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(0)]
    public required string Url { get; set; }

    /// <summary>
    /// Image description intended for screen readers
    /// </summary>
    [JsonPropertyName("alt")]
    [JsonPropertyOrder(1)]
    public string? Alt { get; set; }

    /// <summary>
    /// Action assigned to specific Attachment
    /// </summary>
    [JsonPropertyName("tap")]
    [JsonPropertyOrder(2)]
    public Action? Tap { get; set; }
}