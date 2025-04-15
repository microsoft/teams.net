using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Carousel = new("Carousel");
    public bool IsCarousel => Carousel.Equals(Value);
}

/// <summary>
/// Controls the type of animation to use to navigate between pages.
/// </summary>
[JsonConverter(typeof(JsonConverter<PageAnimation>))]
public partial class PageAnimation(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly PageAnimation Slide = new("slide");
    public bool IsSlide => Slide.Equals(Value);

    public static readonly PageAnimation CrossFade = new("crossFade");
    public bool IsCrossFade => CrossFade.Equals(Value);

    public static readonly PageAnimation None = new("none");
    public bool IsNone => None.Equals(Value);
}

/// <summary>
/// A carousel with sliding pages.
/// </summary>
public class Carousel(params CarouselPage[] pages) : ContainerElement(CardType.Carousel)
{
    /// <summary>
    /// The minimum height, in pixels, of the container, in the <number>px format.
    /// </summary>
    [JsonPropertyName("minHeight")]
    [JsonPropertyOrder(18)]
    public string? MinHeight { get; set; }

    /// <summary>
    /// Controls the type of animation to use to navigate between pages.
    /// </summary>
    [JsonPropertyName("pageAnimation")]
    [JsonPropertyOrder(19)]
    public PageAnimation? PageAnimation { get; set; }

    /// <summary>
    /// The pages in the carousel.
    /// </summary>
    [JsonPropertyName("pages")]
    [JsonPropertyOrder(20)]
    public IList<CarouselPage> Pages { get; set; } = pages;

    public Carousel WithMinHeight(string value)
    {
        MinHeight = value;
        return this;
    }

    public Carousel WithPageAnimation(PageAnimation value)
    {
        PageAnimation = value;
        return this;
    }

    public Carousel AddPages(params CarouselPage[] value)
    {
        foreach (var page in value)
        {
            Pages.Add(page);
        }

        return this;
    }
}