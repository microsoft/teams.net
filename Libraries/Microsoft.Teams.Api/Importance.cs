using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(JsonConverter<Importance>))]
public class Importance(string value) : StringEnum(value)
{
    public static readonly Importance Low = new("low");
    public bool IsLow => Low.Equals(Value);

    public static readonly Importance Normal = new("normal");
    public bool IsNormal => Normal.Equals(Value);

    public static readonly Importance High = new("high");
    public bool IsHigh => High.Equals(Value);
}