using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(JsonConverter<AspectRatio>))]
public class AspectRatio(string value) : StringEnum(value)
{
    public static readonly AspectRatio WideScreen = new("16:9");
    public bool IsWideScreen => WideScreen.Equals(Value);

    public static readonly AspectRatio Standard = new("4:3");
    public bool IsStandard => Standard.Equals(Value);
}