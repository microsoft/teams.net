using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Icon = new("Icon");
    public bool IsIcon => Icon.Equals(Value);
}

/// <summary>
/// Describes how an icon should be positionally displayed.
/// </summary>
[JsonConverter(typeof(JsonConverter<IconPosition>))]
public partial class IconPosition(string value) : StringEnum(value)
{
    public static readonly IconPosition Before = new("before");
    public bool IsBefore => Before.Equals(Value);

    public static readonly IconPosition After = new("after");
    public bool IsAfter => After.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<IconStyle>))]
public partial class IconStyle(string value) : StringEnum(value)
{
    public static readonly IconStyle Regular = new("Regular");
    public bool IsRegular => Regular.Equals(Value);

    public static readonly IconStyle Filled = new("Filled");
    public bool IsFilled => Filled.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<IconSize>))]
public partial class IconSize(string value) : StringEnum(value)
{
    public static readonly IconSize XXSmall = new("xxSmall");
    public bool IsXXSmall => XXSmall.Equals(Value);

    public static readonly IconSize XSmall = new("xSmall");
    public bool IsXSmall => XSmall.Equals(Value);

    public static readonly IconSize Standard = new("Standard");
    public bool IsStandard => Standard.Equals(Value);

    public static readonly IconSize Medium = new("Medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly IconSize Large = new("Large");
    public bool IsLarge => Large.Equals(Value);

    public static readonly IconSize XLarge = new("xLarge");
    public bool IsXLarge => XLarge.Equals(Value);

    public static readonly IconSize XXLarge = new("xxLarge");
    public bool IsXXLarge => XXLarge.Equals(Value);
}

public class Icon(string name) : Element(CardType.Icon)
{
    /// <summary>
    /// name of the icon.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(12)]
    public string Name { get; set; } = name;

    /// <summary>
    /// size of the icon.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonPropertyOrder(13)]
    public IconSize? Size { get; set; }

    /// <summary>
    /// style of the icon.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(14)]
    public IconStyle? Style { get; set; }

    /// <summary>
    /// color of the icon.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonPropertyOrder(15)]
    public Color? Color { get; set; }

    /// <summary>
    /// select action
    /// </summary>
    [JsonPropertyName("selectAction")]
    [JsonPropertyOrder(16)]
    public SelectAction? SelectAction { get; set; }

    public Icon WithSize(IconSize value)
    {
        Size = value;
        return this;
    }

    public Icon WithStyle(IconStyle value)
    {
        Style = value;
        return this;
    }

    public Icon WithColor(Color value)
    {
        Color = value;
        return this;
    }

    public Icon WithSelectAction(SelectAction value)
    {
        SelectAction = value;
        return this;
    }
}