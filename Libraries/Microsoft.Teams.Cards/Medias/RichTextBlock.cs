using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType RichTextBlock = new("RichTextBlock");
    public bool IsRichTextBlock => RichTextBlock.Equals(Value);
}

/// <summary>
/// Defines an array of inlines, allowing for inline text formatting.
/// </summary>
public class RichTextBlock : Element
{
    /// <summary>
    /// The array of inlines.
    /// </summary>
    [JsonPropertyName("inlines")]
    [JsonPropertyOrder(12)]
    public IList<TextRun> Inlines { get; set; }

    public RichTextBlock() : base(CardType.RichTextBlock)
    {
        Inlines = [];
    }

    public RichTextBlock(params TextRun[] inlines) : base(CardType.RichTextBlock)
    {
        Inlines = [];

        foreach (var inline in inlines)
        {
            Inlines.Add(inline);
        }
    }

    public RichTextBlock(params string[] inlines) : base(CardType.RichTextBlock)
    {
        Inlines = [];

        foreach (var inline in inlines)
        {
            Inlines.Add(new TextRun(inline));
        }
    }

    public RichTextBlock AddText(params TextRun[] value)
    {
        foreach (var inline in value)
        {
            Inlines.Add(inline);
        }

        return this;
    }

    public RichTextBlock AddText(params string[] value)
    {
        foreach (var inline in value)
        {
            Inlines.Add(new TextRun(inline));
        }

        return this;
    }
}