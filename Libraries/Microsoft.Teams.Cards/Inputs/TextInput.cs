using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType TextInput = new("Input.Text");
    public bool IsTextInput => TextInput.Equals(Value);
}

/// <summary>
/// Style hint for text input.
/// </summary>
[JsonConverter(typeof(JsonConverter<TextInputStyle>))]
public partial class TextInputStyle(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly TextInputStyle Text = new("text");
    public bool IsText => Text.Equals(Value);

    public static readonly TextInputStyle Tel = new("tel");
    public bool IsTel => Tel.Equals(Value);

    public static readonly TextInputStyle Url = new("url");
    public bool IsUrl => Url.Equals(Value);

    public static readonly TextInputStyle Email = new("email");
    public bool IsEmail => Email.Equals(Value);

    public static readonly TextInputStyle Password = new("password");
    public bool IsPassword => Password.Equals(Value);
}

/// <summary>
/// Allows a user to enter text.
/// </summary>
public class TextInput(string? value) : InputElement(CardType.TextInput)
{
    /// <summary>
    /// If `true`, allow multiple lines of input.
    /// </summary>
    [JsonPropertyName("isMultiline")]
    [JsonPropertyOrder(18)]
    public bool? IsMultiLine { get; set; }

    /// <summary>
    /// Hint of maximum length characters to collect (may be ignored by some clients).
    /// </summary>
    [JsonPropertyName("maxLength")]
    [JsonPropertyOrder(19)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Description of the input desired. Displayed when no selection has been made.
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonPropertyOrder(20)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Regular expression indicating the required format of this text input.
    /// </summary>
    [JsonPropertyName("regex")]
    [JsonPropertyOrder(21)]
    public string? Regex { get; set; }

    /// <summary>
    /// Style hint for text input.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(22)]
    public TextInputStyle? Style { get; set; }

    /// <summary>
    /// The inline action for the input. Typically displayed to the right of the input. It is strongly recommended to provide an icon on the action (which will be displayed instead of the title of the action).
    /// </summary>
    [JsonPropertyName("inlineAction")]
    [JsonPropertyOrder(23)]
    public Action? InlineAction { get; set; }

    /// <summary>
    /// The initial value for this field.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(21)]
    public string? Value { get; set; } = value;

    public TextInput WithMultiLine(bool value = true)
    {
        IsMultiLine = value;
        return this;
    }

    public TextInput WithMaxLength(int value)
    {
        MaxLength = value;
        return this;
    }

    public TextInput WithPlaceholder(string value)
    {
        Placeholder = value;
        return this;
    }

    public TextInput WithRegex(string value)
    {
        Regex = value;
        return this;
    }

    public TextInput WithRegex(Regex value)
    {
        Regex = value.ToString();
        return this;
    }

    public TextInput WithStyle(TextInputStyle value)
    {
        Style = value;
        return this;
    }

    public TextInput WithInlineAction(Action value)
    {
        InlineAction = value;
        return this;
    }

    public TextInput WithValue(string value)
    {
        Value = value;
        return this;
    }
}