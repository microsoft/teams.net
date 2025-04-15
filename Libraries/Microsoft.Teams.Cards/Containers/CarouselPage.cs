using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType CarouselPage = new("CarouselPage");
    public bool IsCarouselPage => CarouselPage.Equals(Value);
}

/// <summary>
/// A page inside a Carousel element.
/// </summary>
public class CarouselPage(params Element[] items) : ContainerElement(CardType.CarouselPage)
{
    /// <summary>
    /// The card elements to render inside the `CarouselPage`.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonPropertyOrder(18)]
    public IList<Element> Items { get; set; } = items;

    /// <summary>
    /// The maximum height, in pixels, of the container, in the <number>px format. When the content of a container exceeds the container's maximum height, a vertical scrollbar is displayed.
    /// </summary>
    [JsonPropertyName("maxHeight")]
    [JsonPropertyOrder(19)]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The minimum height, in pixels, of the container, in the <number>px format.
    /// </summary>
    [JsonPropertyName("minHeight")]
    [JsonPropertyOrder(20)]
    public string? MinHeight { get; set; }

    /// <summary>
    /// The style of the container. Container styles control the colors of the background, border and text inside the container, in such a way that contrast requirements are always met.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(21)]
    public ContainerStyle? Style { get; set; }

    /// <summary>
    /// Defines how the content should be aligned vertically within the container. When not specified, the value of verticalContentAlignment is inherited from the parent container. If no parent container has verticalContentAlignment set, it defaults to Top.
    /// </summary>
    [JsonPropertyName("verticalContentAlignment")]
    [JsonPropertyOrder(22)]
    public VerticalAlignment? VerticalContentAlignment;

    public CarouselPage WithMaxHeight(string value)
    {
        MaxHeight = value;
        return this;
    }

    public CarouselPage WithMinHeight(string value)
    {
        MinHeight = value;
        return this;
    }

    public CarouselPage WithStyle(ContainerStyle value)
    {
        Style = value;
        return this;
    }

    public CarouselPage WithVerticalAlignment(VerticalAlignment value)
    {
        VerticalContentAlignment = value;
        return this;
    }
}