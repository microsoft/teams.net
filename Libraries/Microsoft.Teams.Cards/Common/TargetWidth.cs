using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Controls for which card width the element should be displayed.
/// If targetWidth isn't specified, the element is rendered at all card widths.
/// Using targetWidth makes it possible to author responsive cards that adapt
/// their layout to the available horizontal space.
/// </summary>
[JsonConverter(typeof(JsonConverter<TargetWidth>))]
public partial class TargetWidth(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly TargetWidth VeryNarrow = new("VeryNarrow");
    public bool IsVeryNarrow => VeryNarrow.Equals(Value);

    public static readonly TargetWidth Narrow = new("Narrow");
    public bool IsNarrow => Narrow.Equals(Value);

    public static readonly TargetWidth Standard = new("Standard");
    public bool IsStandard => Standard.Equals(Value);

    public static readonly TargetWidth Wide = new("Wide");
    public bool IsWide => Wide.Equals(Value);

    public static readonly TargetWidth AtLeastVeryNarrow = new("atLeast:VeryNarrow");
    public bool IsAtLeastVeryNarrow => AtLeastVeryNarrow.Equals(Value);

    public static readonly TargetWidth AtMostVeryNarrow = new("atMost:VeryNarrow");
    public bool IsAtMostVeryNarrow => AtMostVeryNarrow.Equals(Value);

    public static readonly TargetWidth AtLeastNarrow = new("atLeast:Narrow");
    public bool IsAtLeastNarrow => AtLeastNarrow.Equals(Value);

    public static readonly TargetWidth AtMostNarrow = new("atMost:Narrow");
    public bool IsAtMostNarrow => AtMostNarrow.Equals(Value);

    public static readonly TargetWidth AtLeastStandard = new("atLeast:Standard");
    public bool IsAtLeastStandard => AtLeastStandard.Equals(Value);

    public static readonly TargetWidth AtMostStandard = new("atMost:Standard");
    public bool IsAtMostStandard => AtMostStandard.Equals(Value);

    public static readonly TargetWidth AtLeastWide = new("atLeast:Wide");
    public bool IsAtLeastWide => AtLeastWide.Equals(Value);

    public static readonly TargetWidth AtMostWide = new("atMost:Wide");
    public bool IsAtMostWide => AtMostWide.Equals(Value);
}