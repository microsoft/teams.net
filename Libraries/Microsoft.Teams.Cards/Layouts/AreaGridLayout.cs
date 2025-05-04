using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType AreaGridLayout = new("Layout.AreaGrid");
    public bool IsAreaGridLayout => AreaGridLayout.Equals(Value);
}

/// <summary>
/// A layout that divides a container into named areas into which elements can be placed.
/// </summary>
public class AreaGridLayout(params GridArea[] areas) : Layout(CardType.AreaGridLayout)
{
    /// <summary>
    /// The areas in the grid layout.
    /// </summary>
    [JsonPropertyName("areas")]
    [JsonPropertyOrder(2)]
    public IList<GridArea> Areas { get; set; } = areas;

    /// <summary>
    /// The columns in the grid layout, defined as a percentage of the available width or in pixels using the <number>px format.
    /// </summary>
    [JsonPropertyName("columns")]
    [JsonPropertyOrder(3)]
    public IList<IUnion<string, int>> Columns { get; set; } = [];

    /// <summary>
    /// The space between columns.
    /// </summary>
    [JsonPropertyName("columnSpacing")]
    [JsonPropertyOrder(4)]
    public Spacing? ColumnSpacing { get; set; }

    /// <summary>
    /// Controls for which card width the layout should be used.
    /// </summary>
    [JsonPropertyName("rowSpacing")]
    [JsonPropertyOrder(5)]
    public Spacing? RowSpacing { get; set; }

    public AreaGridLayout WithColumnSpacing(Spacing value)
    {
        ColumnSpacing = value;
        return this;
    }

    public AreaGridLayout WithRowSpacing(Spacing value)
    {
        RowSpacing = value;
        return this;
    }

    public AreaGridLayout AddAreas(params GridArea[] value)
    {
        foreach (var area in value)
        {
            Areas.Add(area);
        }

        return this;
    }

    public AreaGridLayout AddColumns(params int[] value)
    {
        foreach (var column in value)
        {
            Columns.Add(new Union<string, int>(column));
        }

        return this;
    }

    public AreaGridLayout AddColumns(params string[] value)
    {
        foreach (var column in value)
        {
            Columns.Add(new Union<string, int>(column));
        }

        return this;
    }
}

/// <summary>
/// Defines an area in a Layout.AreaGrid layout.
/// </summary>
public class GridArea
{
    /// <summary>
    /// The start column index of the area. Column indices start at 1.
    /// </summary>
    [JsonPropertyName("column")]
    [JsonPropertyOrder(0)]
    public int? Column { get; set; }

    /// <summary>
    /// Defines how many columns the area should span.
    /// </summary>
    [JsonPropertyName("columnSpan")]
    [JsonPropertyOrder(1)]
    public int? ColumnSpan { get; set; }

    /// <summary>
    /// The name of the area. To place an element in this area, set its grid.area property to match the name of the area.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(2)]
    public string? Name { get; set; }

    /// <summary>
    /// The start row index of the area. Row indices start at 1.
    /// </summary>
    [JsonPropertyName("row")]
    [JsonPropertyOrder(3)]
    public int? Row { get; set; }

    /// <summary>
    /// Defines how many rows the area should span.
    /// </summary>
    [JsonPropertyName("rowSpan")]
    [JsonPropertyOrder(4)]
    public int? RowSpan { get; set; }

    public GridArea WithColumn(int value, int? span)
    {
        Column = value;

        if (span is not null)
        {
            ColumnSpan = span;
        }

        return this;
    }

    public GridArea WithColumnSpan(int value)
    {
        ColumnSpan = value;
        return this;
    }

    public GridArea WithName(string value)
    {
        Name = value;
        return this;
    }

    public GridArea WithRow(int value, int? span)
    {
        Row = value;

        if (span is not null)
        {
            RowSpan = span;
        }

        return this;
    }

    public GridArea WithRowSpan(int value)
    {
        RowSpan = value;
        return this;
    }
}