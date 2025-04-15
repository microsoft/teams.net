using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// `\"auto\"`, `\"stretch\"`, a number representing relative width of the column in the column group, or in version 1.1 and higher, a specific pixel width, like `\"50px\"`.
/// </summary>
[JsonConverter(typeof(JsonConverter<Height>))]
public partial class Height(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly Height Auto = new("auto");
    public bool IsAuto => Auto.Equals(Value);

    public static readonly Height Stretch = new("stretch");
    public bool IsStretch => Stretch.Equals(Value);
}