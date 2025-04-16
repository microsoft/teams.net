using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType DateInput = new("Input.Date");
    public bool IsDateInput => DateInput.Equals(Value);
}

/// <summary>
/// Lets a user choose a date.
/// </summary>
public class DateInput : InputElement
{
    /// <summary>
    /// Hint of maximum value expressed in YYYY-MM-DD(may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("max")]
    [JsonPropertyOrder(18)]
    public string? Max { get; set; }

    /// <summary>
    /// Hint of minimum value expressed in YYYY-MM-DD(may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("min")]
    [JsonPropertyOrder(19)]
    public string? Min { get; set; }

    /// <summary>
    /// Description of the input desired. Displayed when no selection has been made.
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonPropertyOrder(20)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The initial value for this field expressed in YYYY-MM-DD.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(21)]
    public string? Value { get; set; }

    public DateInput() : base(CardType.DateInput)
    {

    }

    public DateInput(string value) : base(CardType.DateInput)
    {
        Value = value;
    }

    public DateInput(DateTime value) : base(CardType.DateInput)
    {
        Value = value.ToShortDateString();
    }

    public DateInput WithMax(string value)
    {
        Max = value;
        return this;
    }

    public DateInput WithMax(DateTime value)
    {
        Max = value.ToShortDateString();
        return this;
    }

    public DateInput WithMin(string value)
    {
        Min = value;
        return this;
    }

    public DateInput WithMin(DateTime value)
    {
        Min = value.ToShortDateString();
        return this;
    }

    public DateInput WithPlaceholder(string value)
    {
        Placeholder = value;
        return this;
    }

    public DateInput WithValue(string value)
    {
        Value = value;
        return this;
    }

    public DateInput WithValue(DateTime value)
    {
        Value = value.ToShortDateString();
        return this;
    }
}