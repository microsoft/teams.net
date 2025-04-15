using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ProgressBar = new("ProgressBar");
    public bool IsProgressBar => ProgressBar.Equals(Value);
}

/// <summary>
/// A progress bar element, to represent a value within a range.
/// </summary>
public class ProgressBar() : Element(CardType.ProgressBar)
{
    /// <summary>
    /// the fill color of the progress bar
    /// </summary>
    [JsonPropertyName("color")]
    [JsonPropertyOrder(12)]
    public Color? Color { get; set; }

    /// <summary>
    /// the current progress value
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(13)]
    public int? Value { get; set; }

    /// <summary>
    /// the max progress value
    /// </summary>
    [JsonPropertyName("max")]
    [JsonPropertyOrder(14)]
    public int? Max { get; set; }

    public ProgressBar WithColor(Color value)
    {
        Color = value;
        return this;
    }

    public ProgressBar WithValue(int value)
    {
        Value = value;
        return this;
    }

    public ProgressBar WithMax(int value)
    {
        Max = value;
        return this;
    }
}