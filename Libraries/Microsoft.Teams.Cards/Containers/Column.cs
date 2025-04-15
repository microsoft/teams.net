using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Column = new("Column");
    public bool IsColumn => Column.Equals(Value);
}

/// <summary>
/// Defines a container that is part of a ColumnSet.
/// </summary>
public class Column(params Element[] items) : ContainerElement(CardType.Column)
{
    [JsonPropertyName("items")]
    [JsonPropertyOrder(18)]
    public IList<Element> Items { get; set; } = items;

    /// <summary>
    /// The minimum height, in pixels, of the container, in the <number>px format.
    /// </summary>
    [JsonPropertyName("minHeight")]
    [JsonPropertyOrder(19)]
    public string? MinHeight { get; set; }

    /// <summary>
    /// Style hint for `Container`.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(20)]
    public ContainerStyle? Style { get; set; }

    /// <summary>
    /// Defines how the content should be aligned vertically within the container. When not specified, the value of verticalContentAlignment is inherited from the parent container. If no parent container has verticalContentAlignment set, it defaults to Top.
    /// </summary>
    [JsonPropertyName("verticalContentAlignment")]
    [JsonPropertyOrder(22)]
    public VerticalAlignment? VerticalContentAlignment;

    /// <summary>
    /// `\"auto\"`, `\"stretch\"`, a number representing relative width of the column in the column group, or in version 1.1 and higher, a specific pixel width, like `\"50px\"`.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonPropertyOrder(23)]
    public Width? Width { get; set; }

    public Column WithStyle(ContainerStyle value)
    {
        Style = value;
        return this;
    }

    public Column WithVerticalAlignment(VerticalAlignment value)
    {
        VerticalContentAlignment = value;
        return this;
    }

    public Column WithMinHeight(string value)
    {
        MinHeight = value;
        return this;
    }

    public Column WithWidth(Width value)
    {
        Width = value;
        return this;
    }

    public Column AddCards(params Element[] value)
    {
        foreach (var card in value)
        {
            Items.Add(card);
        }

        return this;
    }
}