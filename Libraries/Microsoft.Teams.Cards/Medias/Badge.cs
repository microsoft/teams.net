using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Badge = new("Badge");
    public bool IsBadge => Badge.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<BadgeAppearance>))]
public partial class BadgeAppearance(string value) : StringEnum(value)
{
    public static readonly BadgeAppearance Filled = new("filled");
    public bool IsFilled => Filled.Equals(Value);

    public static readonly BadgeAppearance Tint = new("tint");
    public bool IsTint => Tint.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<BadgeStyle>))]
public partial class BadgeStyle(string value) : StringEnum(value)
{
    public static readonly BadgeStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly BadgeStyle Subtle = new("subtle");
    public bool IsSubtle => Subtle.Equals(Value);

    public static readonly BadgeStyle Informative = new("informative");
    public bool IsInformative => Informative.Equals(Value);

    public static readonly BadgeStyle Accent = new("accent");
    public bool IsAccent => Accent.Equals(Value);

    public static readonly BadgeStyle Good = new("good");
    public bool IsGood => Good.Equals(Value);

    public static readonly BadgeStyle Attention = new("attention");
    public bool IsAttention => Attention.Equals(Value);

    public static readonly BadgeStyle Warning = new("warning");
    public bool IsWarning => Warning.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<BadgeShape>))]
public partial class BadgeShape(string value) : StringEnum(value)
{
    public static readonly BadgeShape Square = new("square");
    public bool IsSquare => Square.Equals(Value);

    public static readonly BadgeShape Rounded = new("rounded");
    public bool IsRounded => Rounded.Equals(Value);

    public static readonly BadgeShape Circular = new("circular");
    public bool IsCircular => Circular.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<BadgeSize>))]
public partial class BadgeSize(string value) : StringEnum(value)
{
    public static readonly BadgeSize Medium = new("medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly BadgeSize Large = new("large");
    public bool IsLarge => Large.Equals(Value);

    public static readonly BadgeSize ExtraLarge = new("extraLarge");
    public bool IsExtraLarge => ExtraLarge.Equals(Value);
}

/// <summary>
/// A badge element to show an icon and/or text in a compact form over a colored background.
/// </summary>
public class Badge() : Element(CardType.Badge)
{
    /// <summary>
    /// Controls the strength of the background color.
    /// </summary>
    [JsonPropertyName("appearance")]
    [JsonPropertyOrder(12)]
    public BadgeAppearance? Appearance { get; set; }

    /// <summary>
    /// The name of the icon to display.
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonPropertyOrder(13)]
    public string? Icon { get; set; }

    /// <summary>
    /// Controls the position of the icon.
    /// </summary>
    [JsonPropertyName("iconPosition")]
    [JsonPropertyOrder(14)]
    public IconPosition? IconPosition { get; set; }

    /// <summary>
    /// Controls the shape of the badge.
    /// </summary>
    [JsonPropertyName("shape")]
    [JsonPropertyOrder(15)]
    public BadgeShape? Shape { get; set; }

    /// <summary>
    /// The size of the badge.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonPropertyOrder(16)]
    public BadgeSize? Size { get; set; }

    /// <summary>
    /// The style of the badge.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(17)]
    public BadgeStyle? Style { get; set; }

    /// <summary>
    /// The text to display.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(18)]
    public string? Text { get; set; }

    /// <summary>
    /// Controls the tooltip text to display when the badge is hovered over.
    /// </summary>
    [JsonPropertyName("tooltip")]
    [JsonPropertyOrder(19)]
    public string? Tooltip { get; set; }

    public Badge WithAppearance(BadgeAppearance value)
    {
        Appearance = value;
        return this;
    }

    public Badge WithIcon(string value, IconPosition? position)
    {
        Icon = value;

        if (position is not null)
        {
            IconPosition = position;
        }

        return this;
    }

    public Badge WithIconPosition(IconPosition value)
    {
        IconPosition = value;
        return this;
    }

    public Badge WithShape(BadgeShape value)
    {
        Shape = value;
        return this;
    }

    public Badge WithSize(BadgeSize value)
    {
        Size = value;
        return this;
    }

    public Badge WithStyle(BadgeStyle value)
    {
        Style = value;
        return this;
    }

    public Badge WithText(string value)
    {
        Text = value;
        return this;
    }

    public Badge WithTooltip(string value)
    {
        Tooltip = value;
        return this;
    }
}