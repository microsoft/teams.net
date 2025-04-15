using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

[JsonConverter(typeof(JsonConverter<Color>))]
public partial class Color(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly Color Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly Color Dark = new("dark");
    public bool IsDark => Dark.Equals(Value);

    public static readonly Color Light = new("light");
    public bool IsLight => Light.Equals(Value);

    public static readonly Color Accent = new("accent");
    public bool IsAccent => Accent.Equals(Value);

    public static readonly Color Good = new("good");
    public bool IsGood => Good.Equals(Value);

    public static readonly Color Warning = new("warning");
    public bool IsWarning => Warning.Equals(Value);

    public static readonly Color Attention = new("attention");
    public bool IsAttention => Attention.Equals(Value);
}