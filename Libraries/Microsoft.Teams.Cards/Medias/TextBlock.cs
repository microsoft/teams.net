using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType TextBlock = new("TextBlock");
    public bool IsTextBlock => TextBlock.Equals(Value);
}

/// <summary>
///  The style of this TextBlock for accessibility purposes.
/// </summary>
[JsonConverter(typeof(JsonConverter<TextBlockStyle>))]
public partial class TextBlockStyle(string value) : StringEnum(value)
{
    public static readonly TextBlockStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly TextBlockStyle Heading = new("heading");
    public bool IsHeading => Heading.Equals(Value);
}

/// <summary>
/// Displays text, allowing control over font sizes, weight, and color.
/// </summary>
public class TextBlock(string text) : Element(CardType.TextBlock)
{
    /// <summary>
    /// Text to display. A subset of markdown is supported (https://aka.ms/ACTextFeatures)
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(12)]
    public string Text { get; set; } = text;

    /// <summary>
    /// The style of this TextBlock for accessibility purposes.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(13)]
    public TextBlockStyle? Style { get; set; }

    /// <summary>
    /// Controls the color of TextBlock elements.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonPropertyOrder(14)]
    public Color? Color { get; set; }

    /// <summary>
    /// Type of font to use for rendering
    /// </summary>
    [JsonPropertyName("fontType")]
    [JsonPropertyOrder(15)]
    public FontType? FontType { get; set; }

    /// <summary>
    /// If true, displays text slightly toned down to appear less prominent.
    /// </summary>
    [JsonPropertyName("isSubtle")]
    [JsonPropertyOrder(16)]
    public bool? IsSubtle { get; set; }

    /// <summary>
    /// Specifies the maximum number of lines to display.
    /// </summary>
    [JsonPropertyName("maxLines")]
    [JsonPropertyOrder(17)]
    public int? MaxLines { get; set; }

    /// <summary>
    /// Controls size of text.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonPropertyOrder(18)]
    public FontSize? Size { get; set; }

    /// <summary>
    /// Controls the weight of TextBlock elements.
    /// </summary>
    [JsonPropertyName("weight")]
    [JsonPropertyOrder(19)]
    public FontWeight? Weight { get; set; }

    /// <summary>
    /// If true, allow text to wrap. Otherwise, text is clipped.
    /// </summary>
    [JsonPropertyName("wrap")]
    [JsonPropertyOrder(20)]
    public bool? Wrap { get; set; }

    public TextBlock WithStyle(TextBlockStyle value)
    {
        Style = value;
        return this;
    }

    public TextBlock WithColor(Color value)
    {
        Color = value;
        return this;
    }

    public TextBlock WithFontType(FontType value)
    {
        FontType = value;
        return this;
    }

    public TextBlock WithSubtle(bool value = true)
    {
        IsSubtle = value;
        return this;
    }

    public TextBlock WithMaxLines(int value)
    {
        MaxLines = value;
        return this;
    }

    public TextBlock WithSize(FontSize value)
    {
        Size = value;
        return this;
    }

    public TextBlock WithWeight(FontWeight value)
    {
        Weight = value;
        return this;
    }

    public TextBlock WithWrap(bool value = true)
    {
        Wrap = value;
        return this;
    }

    public TextBlock AddText(params string[] value)
    {
        Text += string.Join("", value);
        return this;
    }
}