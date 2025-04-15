using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Cards;

/// <summary>
/// any card
/// </summary>
public abstract class Card
{
    /// <summary>
    /// Title of this card
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(0)]
    public string? Title { get; set; }

    /// <summary>
    /// Subtitle of this card
    /// </summary>
    [JsonPropertyName("subtitle")]
    [JsonPropertyOrder(1)]
    public string? SubTitle { get; set; }

    /// <summary>
    /// Text of this card
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(2)]
    public string? Text { get; set; }

    /// <summary>
    /// Actions on this card
    /// </summary>
    [JsonPropertyName("buttons")]
    [JsonPropertyOrder(3)]
    public IList<Action>? Buttons { get; set; }
}