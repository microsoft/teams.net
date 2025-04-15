using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ColumnSet = new("ColumnSet");
    public bool IsColumnSet => ColumnSet.Equals(Value);
}

/// <summary>
/// ColumnSet divides a region into Columns, allowing elements to sit side-by-side.
/// </summary>
public class ColumnSet(params Column[] columns) : ContainerElement(CardType.ColumnSet)
{
    /// <summary>
    /// The array of `Columns` to divide the region into.
    /// </summary>
    [JsonPropertyName("columns")]
    [JsonPropertyOrder(18)]
    public IList<Column> Columns { get; set; } = columns;

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
    [JsonPropertyName("horizontalContentAlignment")]
    [JsonPropertyOrder(22)]
    public HorizontalAlignment? HorizontalContentAlignment;

    public ColumnSet WithStyle(ContainerStyle value)
    {
        Style = value;
        return this;
    }

    public ColumnSet WithHorizontalContentAlignment(HorizontalAlignment value)
    {
        HorizontalContentAlignment = value;
        return this;
    }

    public ColumnSet WithMinHeight(string value)
    {
        MinHeight = value;
        return this;
    }


    public ColumnSet AddColumns(params Column[] value)
    {
        foreach (var card in value)
        {
            Columns.Add(card);
        }

        return this;
    }
}