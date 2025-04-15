using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

/// <summary>
/// controls how elements are displayed
/// </summary>
public abstract class Layout(CardType type)
{
    /// <summary>
    /// the layout card type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public CardType Type { get; set; } = type;

    /// <summary>
    /// Controls for which card width the layout should be used.
    /// </summary>
    [JsonPropertyName("targetWidth")]
    [JsonPropertyOrder(1)]
    public TargetWidth? TargetWidth { get; set; }

    public Layout WithTargetWidth(TargetWidth value)
    {
        TargetWidth = value;
        return this;
    }
}