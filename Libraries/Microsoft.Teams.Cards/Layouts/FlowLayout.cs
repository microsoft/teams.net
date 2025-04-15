using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType FlowLayout = new("Layout.Flow");
    public bool IsFlowLayout => FlowLayout.Equals(Value);
}

/// <summary>
/// Controls how item should fit inside the container.
/// </summary>
[JsonConverter(typeof(JsonConverter<FlowLayoutItemFit>))]
public partial class FlowLayoutItemFit(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly FlowLayoutItemFit Fit = new("Fit");
    public bool IsFit => Fit.Equals(Value);

    public static readonly FlowLayoutItemFit Fill = new("Fill");
    public bool IsFill => Fill.Equals(Value);
}

/// <summary>
/// A layout that spreads elements horizontally and wraps them across multiple rows, as needed.
/// </summary>
public class FlowLayout() : Layout(CardType.FlowLayout)
{
    /// <summary>
    /// The space between items.
    /// </summary>
    [JsonPropertyName("columnSpacing")]
    [JsonPropertyOrder(2)]
    public Spacing? ColumnSpacing { get; set; }

    /// <summary>
    /// Controls how the content of the container should be horizontally aligned.
    /// </summary>
    [JsonPropertyName("horizontalItemsAlignment")]
    [JsonPropertyOrder(3)]
    public HorizontalAlignment? HorizontalItemsAlignment { get; set; }

    /// <summary>
    /// Controls how item should fit inside the container.
    /// </summary>
    [JsonPropertyName("itemFit")]
    [JsonPropertyOrder(4)]
    public FlowLayoutItemFit? ItemFit { get; set; }

    /// <summary>
    /// The width, in pixels, of each item, in the <number>px format. Should not be used if maxItemWidth and/or minItemWidth are set.
    /// </summary>
    [JsonPropertyName("itemWidth")]
    [JsonPropertyOrder(5)]
    public string? ItemWidth { get; set; }

    /// <summary>
    /// The maximum width, in pixels, of each item, in the <number>px format. Should not be used if itemWidth is set.
    /// </summary>
    [JsonPropertyName("maxItemWidth")]
    [JsonPropertyOrder(6)]
    public string? MaxItemWidth { get; set; }

    /// <summary>
    /// The minimum width, in pixels, of each item, in the <number>px format. Should not be used if itemWidth is set.
    /// </summary>
    [JsonPropertyName("minItemWidth")]
    [JsonPropertyOrder(7)]
    public string? MinItemWidth { get; set; }

    /// <summary>
    /// The space between rows of items.
    /// </summary>
    [JsonPropertyName("rowSpacing")]
    [JsonPropertyOrder(8)]
    public Spacing? RowSpacing { get; set; }

    /// <summary>
    /// Controls how the content of the container should be vertically aligned.
    /// </summary>
    [JsonPropertyName("verticalItemsAlignment")]
    [JsonPropertyOrder(9)]
    public VerticalAlignment? VerticalItemsAlignment { get; set; }

    public FlowLayout WithColumnSpacing(Spacing value)
    {
        ColumnSpacing = value;
        return this;
    }

    public FlowLayout WithHorizontalAlignment(HorizontalAlignment value)
    {
        HorizontalItemsAlignment = value;
        return this;
    }

    public FlowLayout WithItemFit(FlowLayoutItemFit value)
    {
        ItemFit = value;
        return this;
    }

    public FlowLayout WithItemWidth(string value)
    {
        ItemWidth = value;
        return this;
    }

    public FlowLayout WithItemMinWidth(string value)
    {
        MinItemWidth = value;
        return this;
    }

    public FlowLayout WithItemMaxWidth(string value)
    {
        MaxItemWidth = value;
        return this;
    }

    public FlowLayout WithRowSpacing(Spacing value)
    {
        RowSpacing = value;
        return this;
    }

    public FlowLayout WithVerticalAlignment(VerticalAlignment value)
    {
        VerticalItemsAlignment = value;
        return this;
    }
}