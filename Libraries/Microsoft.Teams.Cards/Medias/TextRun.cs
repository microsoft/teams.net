using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType TextRun = new("TextRun");
    public bool IsTextRun => TextRun.Equals(Value);
}

/// <summary>
/// Defines a single run of formatted text. A TextRun with no properties set can be represented in the json as string containing the text as a shorthand for the json object. These two representations are equivalent.
/// </summary>
public class TextRun(string text)
{
    /// <summary>
    /// the card type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public CardType Type { get; set; } = CardType.TextRun;

    /// <summary>
    /// Text to display. Markdown is not supported.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(1)]
    public string Text { get; set; } = text;

    /// <summary>
    /// Controls the color of the text.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonPropertyOrder(2)]
    public Color? Color { get; set; }

    /// <summary>
    /// The type of font to use
    /// </summary>
    [JsonPropertyName("fontType")]
    [JsonPropertyOrder(3)]
    public FontType? FontType { get; set; }

    /// <summary>
    /// If true, displays the text highlighted.
    /// </summary>
    [JsonPropertyName("highlight")]
    [JsonPropertyOrder(4)]
    public bool? Highlight { get; set; }

    /// <summary>
    /// If true, displays text slightly toned down to appear less prominent.
    /// </summary>
    [JsonPropertyName("isSubtle")]
    [JsonPropertyOrder(5)]
    public bool? IsSubtle { get; set; }

    /// <summary>
    /// If true, displays the text using italic font.
    /// </summary>
    [JsonPropertyName("italic")]
    [JsonPropertyOrder(6)]
    public bool? Italic { get; set; }

    /// <summary>
    /// Action to invoke when this text run is clicked. Visually changes the text run into a hyperlink. Action.ShowCard is not supported.
    /// </summary>
    [JsonPropertyName("selectAction")]
    [JsonPropertyOrder(7)]
    public SelectAction? SelectAction { get; set; }

    /// <summary>
    /// Controls size of text.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonPropertyOrder(8)]
    public FontSize? Size { get; set; }

    /// <summary>
    /// If true, displays the text with strikethrough.
    /// </summary>
    [JsonPropertyName("strikeThrough")]
    [JsonPropertyOrder(9)]
    public bool? StrikeThrough { get; set; }

    /// <summary>
    /// If true, displays the text with an underline.
    /// </summary>
    [JsonPropertyName("underline")]
    [JsonPropertyOrder(10)]
    public bool? Underline { get; set; }

    /// <summary>
    /// Controls the weight of the text.
    /// </summary>
    [JsonPropertyName("weight")]
    [JsonPropertyOrder(11)]
    public FontWeight? Weight { get; set; }

    public TextRun WithColor(Color value)
    {
        Color = value;
        return this;
    }

    public TextRun WithFontType(FontType value)
    {
        FontType = value;
        return this;
    }

    public TextRun WithHighlight(bool value = true)
    {
        Highlight = value;
        return this;
    }

    public TextRun WithSubtle(bool value = true)
    {
        IsSubtle = value;
        return this;
    }

    public TextRun WithItalic(bool value = true)
    {
        Italic = value;
        return this;
    }

    public TextRun WithSelectAction(SelectAction value)
    {
        SelectAction = value;
        return this;
    }

    public TextRun WithSize(FontSize value)
    {
        Size = value;
        return this;
    }

    public TextRun WithStrikeThrough(bool value = true)
    {
        StrikeThrough = value;
        return this;
    }

    public TextRun WithUnderline(bool value = true)
    {
        Underline = value;
        return this;
    }

    public TextRun WithWeight(FontWeight value)
    {
        Weight = value;
        return this;
    }

    public TextRun AddText(params string[] value)
    {
        Text += string.Join("", value);
        return this;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}