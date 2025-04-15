using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Determines the position of the label. It can take 'inline' and 'above' values. By default, the label is placed 'above' when label position is not specified.
/// </summary>
[JsonConverter(typeof(JsonConverter<InputLabelPosition>))]
public partial class InputLabelPosition(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly InputLabelPosition Inline = new("inline");
    public bool IsInline => Inline.Equals(Value);

    public static readonly InputLabelPosition Above = new("above");
    public bool IsAbove => Above.Equals(Value);
}

/// <summary>
/// Style hint for input fields. Allows input fields to appear as read-only but when user clicks/focuses on the field, it allows them to update those fields.
/// </summary>
[JsonConverter(typeof(JsonConverter<InputStyle>))]
public partial class InputStyle(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly InputStyle RevealOnHover = new("revealOnHover");
    public bool IsRevealOnHover => RevealOnHover.Equals(Value);

    public static readonly InputStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);
}

/// <summary>
/// any element that can accept user input
/// </summary>
public abstract class InputElement(CardType type) : Element(type)
{
    /// <summary>
    /// Error message to display when entered input is invalid
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonPropertyOrder(12)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether or not this input is required
    /// </summary>
    [JsonPropertyName("isRequired")]
    [JsonPropertyOrder(13)]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// Label for this input
    /// </summary>
    [JsonPropertyName("label")]
    [JsonPropertyOrder(14)]
    public string? Label { get; set; }

    /// <summary>
    /// Determines the position of the label. It can take 'inline' and 'above' values. By default, the label is placed 'above' when label position is not specified.
    /// </summary>
    [JsonPropertyName("labelPosition")]
    [JsonPropertyOrder(15)]
    public InputLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Determines the width of the label in percent like 40 or a specific pixel width like ‘40px’ when label is placed inline with the input. labelWidth would be ignored when the label is displayed above the input.
    /// </summary>
    [JsonPropertyName("labelWidth")]
    [JsonPropertyOrder(16)]
    public IUnion<string, int>? LabelWidth { get; set; }

    /// <summary>
    /// Style hint for input fields. Allows input fields to appear as read-only but when user clicks/focuses on the field, it allows them to update those fields.
    /// </summary>
    [JsonPropertyName("inputStyle")]
    [JsonPropertyOrder(17)]
    public InputStyle? InputStyle { get; set; }

    public InputElement WithError(string value)
    {
        ErrorMessage = value;
        return this;
    }

    public InputElement WithRequired(bool value = true)
    {
        IsRequired = value;
        return this;
    }

    public InputElement WithLabel(string value, InputLabelPosition? position, IUnion<string, int>? width)
    {
        Label = value;

        if (position != null)
        {
            LabelPosition = position;
        }

        if (width != null)
        {
            LabelWidth = width;
        }

        return this;
    }

    public InputElement WithLabelPosition(InputLabelPosition value)
    {
        LabelPosition = value;
        return this;
    }

    public InputElement WithLabelWidth(IUnion<string, int> value)
    {
        LabelWidth = value;
        return this;
    }

    public InputElement WithInputStyle(InputStyle value)
    {
        InputStyle = value;
        return this;
    }
}