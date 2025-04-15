using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ToggleInput = new("Input.Toggle");
    public bool IsToggleInput => ToggleInput.Equals(Value);
}

/// <summary>
/// Allows a user to enter a number.
/// </summary>
public class ToggleInput : InputElement
{
    /// <summary>
    /// Title for the toggle
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(18)]
    public string Title { get; set; }

    /// <summary>
    /// The initial selected value. If you want the toggle to be initially on, set this to the value of valueOn‘s value.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(19)]
    public StringBool? Value { get; set; }

    /// <summary>
    /// The value when toggle is off
    /// </summary>
    [JsonPropertyName("valueOff")]
    [JsonPropertyOrder(20)]
    public StringBool? ValueOff { get; set; }

    /// <summary>
    /// The value when toggle is on
    /// </summary>
    [JsonPropertyName("valueOn")]
    [JsonPropertyOrder(21)]
    public StringBool? ValueOn { get; set; }

    /// <summary>
    /// If `true`, allow text to wrap. Otherwise, text is clipped.
    /// </summary>
    [JsonPropertyName("wrap")]
    [JsonPropertyOrder(22)]
    public bool? Wrap { get; set; }

    public ToggleInput(string title) : base(CardType.ToggleInput)
    {
        Title = title;
    }

    public ToggleInput(string title, bool value) : base(CardType.ToggleInput)
    {
        Title = title;
        Value = value == true ? StringBool.True : StringBool.False;
    }

    public ToggleInput(string title, string value) : base(CardType.ToggleInput)
    {
        Title = title;
        Value = new(value);
    }

    public ToggleInput WithValue(bool value)
    {
        Value = value == true ? StringBool.True : StringBool.False;
        return this;
    }

    public ToggleInput WithValue(string value)
    {
        Value = new(value);
        return this;
    }

    public ToggleInput WithValueOff(bool value)
    {
        ValueOff = value == true ? StringBool.True : StringBool.False;
        return this;
    }

    public ToggleInput WithValueOff(string value)
    {
        ValueOff = new(value);
        return this;
    }

    public ToggleInput WithValueOn(bool value)
    {
        ValueOn = value == true ? StringBool.True : StringBool.False;
        return this;
    }

    public ToggleInput WithValueOn(string value)
    {
        ValueOn = new(value);
        return this;
    }

    public ToggleInput WithWrap(bool value = true)
    {
        Wrap = value;
        return this;
    }
}