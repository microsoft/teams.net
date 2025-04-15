using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// `\"auto\"`, `\"stretch\"`, a number representing relative width of the column in the column group, or in version 1.1 and higher, a specific pixel width, like `\"50px\"`.
/// </summary>
[JsonConverter(typeof(JsonConverter<Width>))]
public partial class Width(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly Width Auto = new("auto");
    public bool IsAuto => Auto.Equals(Value);

    public static readonly Width Stretch = new("stretch");
    public bool IsStretch => Stretch.Equals(Value);
}