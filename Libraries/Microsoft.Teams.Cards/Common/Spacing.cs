using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Specifies how much spacing. Hosts pick the exact pixel amounts for each of these.
/// </summary>
[JsonConverter(typeof(JsonConverter<Spacing>))]
public partial class Spacing(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly Spacing Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly Spacing None = new("none");
    public bool IsNone => None.Equals(Value);

    public static readonly Spacing Small = new("small");
    public bool IsSmall => Small.Equals(Value);

    public static readonly Spacing Medium = new("medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly Spacing Large = new("large");
    public bool IsLarge => Large.Equals(Value);

    public static readonly Spacing ExtraLarge = new("extraLarge");
    public bool IsExtraLarge => ExtraLarge.Equals(Value);

    public static readonly Spacing Padding = new("padding");
    public bool IsPadding => Padding.Equals(Value);
}