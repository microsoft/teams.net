using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType TimeInput = new("Input.Time");
    public bool IsTimeInput => TimeInput.Equals(Value);
}

/// <summary>
/// Lets a user select a time.
/// </summary>
public class TimeInput(string? value) : InputElement(CardType.TimeInput)
{
    /// <summary>
    /// Hint of maximum value expressed in HH:MM (may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("max")]
    [JsonPropertyOrder(18)]
    public string? Max { get; set; }

    /// <summary>
    /// Hint of minimum value expressed in HH:MM (may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("min")]
    [JsonPropertyOrder(19)]
    public string? Min { get; set; }

    /// <summary>
    /// Description of the input desired. Displayed when no time has been selected.
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonPropertyOrder(20)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The initial value for this field expressed in HH:MM.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(21)]
    public string? Value { get; set; } = value;

    public TimeInput WithMax(string value)
    {
        Max = value;
        return this;
    }

    public TimeInput WithMax(DateTime value)
    {
        Max = value.ToShortTimeString();
        return this;
    }

    public TimeInput WithMin(string value)
    {
        Min = value;
        return this;
    }

    public TimeInput WithMin(DateTime value)
    {
        Min = value.ToShortTimeString();
        return this;
    }

    public TimeInput WithPlaceholder(string value)
    {
        Placeholder = value;
        return this;
    }

    public TimeInput WithValue(string value)
    {
        Value = value;
        return this;
    }
}