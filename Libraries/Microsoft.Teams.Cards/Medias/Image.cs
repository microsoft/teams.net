using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Image = new("Image");
    public bool IsImage => Image.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<ImageStyle>))]
public partial class ImageStyle(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly ImageStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly ImageStyle Person = new("person");
    public bool IsPerson => Person.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<ImageSize>))]
public partial class ImageSize(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly ImageSize Auto = new("auto");
    public bool IsAuto => Auto.Equals(Value);

    public static readonly ImageSize Stretch = new("stretch");
    public bool IsStretch => Stretch.Equals(Value);

    public static readonly ImageSize Small = new("small");
    public bool IsSmall => Small.Equals(Value);

    public static readonly ImageSize Medium = new("medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly ImageSize Large = new("large");
    public bool IsLarge => Large.Equals(Value);
}

/// <summary>
/// Displays an image. Acceptable formats are PNG, JPEG, and GIF
/// </summary>
public class Image(string url) : Element(CardType.Image)
{
    /// <summary>
    /// The URL to the image. Supports data URI in version 1.2+
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(12)]
    public string Url { get; set; } = url;

    /// <summary>
    /// Alternate text describing the image.
    /// </summary>
    [JsonPropertyName("altText")]
    [JsonPropertyOrder(13)]
    public string? AltText { get; set; }

    /// <summary>
    /// Controls if the image can be expanded to full screen.
    /// </summary>
    [JsonPropertyName("allowExpand")]
    [JsonPropertyOrder(14)]
    public bool? AllowExpand { get; set; }

    /// <summary>
    /// Applies a background to a transparent image. This property will respect the image style.
    /// </summary>
    [JsonPropertyName("backgroundColor")]
    [JsonPropertyOrder(15)]
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// An Action that will be invoked when the Image is tapped or selected. Action.ShowCard is not supported.
    /// </summary>
    [JsonPropertyName("selectAction")]
    [JsonPropertyOrder(16)]
    public SelectAction? SelectAction { get; set; }

    /// <summary>
    /// Controls the approximate size of the image. The physical dimensions will vary per host.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonPropertyOrder(17)]
    public ImageSize? Size { get; set; }

    /// <summary>
    /// Controls how this Image is displayed.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(18)]
    public ImageStyle? Style { get; set; }

    /// <summary>
    /// The desired on-screen width of the image, ending in ‘px’. E.g., 50px. This overrides the size property.
    /// </summary>
    [JsonPropertyName("width")]
    [JsonPropertyOrder(19)]
    public string? Width { get; set; }

    public Image WithUrl(string value)
    {
        Url = value;
        return this;
    }

    public Image WithAltText(string value)
    {
        AltText = value;
        return this;
    }

    public Image WithAllowExpand(bool value = true)
    {
        AllowExpand = value;
        return this;
    }

    public Image WithBackgroundColor(string value)
    {
        BackgroundColor = value;
        return this;
    }

    public Image WithSelectAction(SelectAction value)
    {
        SelectAction = value;
        return this;
    }

    public Image WithSize(ImageSize value)
    {
        Size = value;
        return this;
    }

    public Image WithStyle(ImageStyle value)
    {
        Style = value;
        return this;
    }

    public Image WithWidth(string value)
    {
        Width = value;
        return this;
    }
}