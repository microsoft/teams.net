using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType NumberInput = new("Input.Number");
    public bool IsNumberInput => NumberInput.Equals(Value);
}

/// <summary>
/// Allows a user to enter a number.
/// </summary>
public class NumberInput(double? value) : InputElement(CardType.NumberInput)
{
    /// <summary>
    /// Hint of maximum value (may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("max")]
    [JsonPropertyOrder(18)]
    public double? Max { get; set; }

    /// <summary>
    /// Hint of minimum value (may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("min")]
    [JsonPropertyOrder(19)]
    public double? Min { get; set; }

    /// <summary>
    /// Description of the input desired. Displayed when no selection has been made.
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonPropertyOrder(20)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Initial value for this field.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(21)]
    public double? Value { get; set; } = value;

    public NumberInput WithMax(double value)
    {
        Max = value;
        return this;
    }

    public NumberInput WithMin(double value)
    {
        Min = value;
        return this;
    }

    public NumberInput WithPlaceholder(string value)
    {
        Placeholder = value;
        return this;
    }

    public NumberInput WithValue(double value)
    {
        Value = value;
        return this;
    }
}