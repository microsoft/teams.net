using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

[JsonConverter(typeof(JsonConverter<FontSize>))]
public partial class FontSize(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly FontSize Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly FontSize Small = new("small");
    public bool IsSmall => Small.Equals(Value);

    public static readonly FontSize Medium = new("medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly FontSize Large = new("large");
    public bool IsLarge => Large.Equals(Value);

    public static readonly FontSize ExtraLarge = new("extraLarge");
    public bool IsExtraLarge => ExtraLarge.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<FontType>))]
public partial class FontType(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly FontType Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly FontType Monospace = new("monospace");
    public bool IsMonospace => Monospace.Equals(Value);
}

[JsonConverter(typeof(JsonConverter<FontWeight>))]
public partial class FontWeight(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly FontWeight Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly FontWeight Lighter = new("lighter");
    public bool IsLighter => Lighter.Equals(Value);

    public static readonly FontWeight Bolder = new("bolder");
    public bool IsBolder => Bolder.Equals(Value);
}