using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Describes how the image should be aligned if it must be cropped or if using repeat fill mode.
/// </summary>
[JsonConverter(typeof(JsonConverter<HorizontalAlignment>))]
public partial class HorizontalAlignment(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly HorizontalAlignment Left = new("left");
    public bool IsLeft => Left.Equals(Value);

    public static readonly HorizontalAlignment Center = new("center");
    public bool IsCenter => Center.Equals(Value);

    public static readonly HorizontalAlignment Right = new("right");
    public bool IsRight => Right.Equals(Value);
}