using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType BackgroundImage = new("BackgroundImage");
    public bool IsBackgroundImage => BackgroundImage.Equals(Value);
}

/// <summary>
/// Describes how the image should fill the area.
/// </summary>
[JsonConverter(typeof(JsonConverter<FillMode>))]
public partial class FillMode(string value) : StringEnum(value)
{
    public static readonly FillMode Cover = new("cover");
    public bool IsCover => Cover.Equals(Value);

    public static readonly FillMode Repeat = new("repeat");
    public bool IsRepeat => Repeat.Equals(Value);

    public static readonly FillMode RepeatHorizontally = new("repeatHorizontally");
    public bool IsRepeatHorizontally => RepeatHorizontally.Equals(Value);

    public static readonly FillMode RepeatVertically = new("repeatVertically");
    public bool IsRepeatVertically => RepeatVertically.Equals(Value);
}

/// <summary>
/// Specifies a background image. Acceptable formats are PNG, JPEG, and GIF
/// </summary>
public class BackgroundImage(string uri)
{
    /// <summary>
    /// the card type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public CardType Type { get; set; } = CardType.BackgroundImage;

    /// <summary>
    /// The URL (or data url) of the image. Acceptable formats are PNG, JPEG, and GIF
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonPropertyOrder(1)]
    public string Uri { get; set; } = uri;

    /// <summary>
    /// Describes how the image should fill the area.
    /// </summary>
    [JsonPropertyName("fillMode")]
    [JsonPropertyOrder(2)]
    public FillMode? FillMode { get; set; }

    /// <summary>
    /// Describes how the image should be aligned if it must be cropped or if using repeat fill mode.
    /// </summary>
    [JsonPropertyName("horizontalAlignment")]
    [JsonPropertyOrder(3)]
    public HorizontalAlignment? HorizontalAlignment { get; set; }

    /// <summary>
    /// Describes how the image should be aligned if it must be cropped or if using repeat fill mode.
    /// </summary>
    [JsonPropertyName("verticalAlignment")]
    [JsonPropertyOrder(4)]
    public VerticalAlignment? VerticalAlignment { get; set; }

    public BackgroundImage WithUri(string value)
    {
        Uri = value;
        return this;
    }

    public BackgroundImage WithFillMode(FillMode value)
    {
        FillMode = value;
        return this;
    }

    public BackgroundImage WithHorizontalAlignment(HorizontalAlignment value)
    {
        HorizontalAlignment = value;
        return this;
    }

    public BackgroundImage WithVerticalAlignment(VerticalAlignment value)
    {
        VerticalAlignment = value;
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