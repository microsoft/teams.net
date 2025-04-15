using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ImageSet = new("ImageSet");
    public bool IsImageSet => ImageSet.Equals(Value);
}

/// <summary>
/// Controls how the images are displayed.
/// </summary>
[JsonConverter(typeof(JsonConverter<ImageSetStyle>))]
public partial class ImageSetStyle(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly ImageSetStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly ImageSetStyle Stacked = new("stacked");
    public bool IsStacked => Stacked.Equals(Value);

    public static readonly ImageSetStyle Grid = new("grid");
    public bool IsGrid => Grid.Equals(Value);
}

/// <summary>
/// The `ImageSet` element displays a collection of `Image`'s similar to a gallery. Acceptable formats are `PNG`, `JPEG`, and `GIF`.
/// </summary>
public class ImageSet(params Image[] images) : Element(CardType.ImageSet)
{
    /// <summary>
    /// Controls how the images are displayed.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(12)]
    public ImageSetStyle? Style { get; set; }

    /// <summary>
    /// Controls the approximate size of each image. The physical dimensions will vary per host.
    /// Auto and stretch are not supported for `ImageSet`. The size will default to medium if
    /// those values are set.
    /// </summary>
    [JsonPropertyName("imageSize")]
    [JsonPropertyOrder(13)]
    public ImageSize? ImageSize { get; set; }

    /// <summary>
    /// The array of `Image`'s to show.
    /// </summary>
    [JsonPropertyName("images")]
    [JsonPropertyOrder(14)]
    public IList<Image> Images { get; set; } = images;

    public ImageSet WithStyle(ImageSetStyle value)
    {
        Style = value;
        return this;
    }

    public ImageSet WithImageSize(ImageSize value)
    {
        ImageSize = value;
        return this;
    }

    public ImageSet AddImages(params Image[] value)
    {
        foreach (var image in value)
        {
            Images.Add(image);
        }

        return this;
    }
}