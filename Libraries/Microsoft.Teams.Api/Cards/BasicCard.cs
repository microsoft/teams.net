using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Cards;

/// <summary>
/// A basic card
/// </summary>
public class BasicCard : Card
{
    /// <summary>
    /// Array of images for the card
    /// </summary>
    [JsonPropertyName("images")]
    [JsonPropertyOrder(4)]
    public IList<Image>? Images { get; set; }

    /// <summary>
    /// This action will be activated when user taps on the card itself
    /// </summary>
    [JsonPropertyName("tap")]
    [JsonPropertyOrder(5)]
    public Action? Tap { get; set; }
}