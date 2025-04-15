using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

/// <summary>
/// a string representation of a boolean value
/// </summary>
[JsonConverter(typeof(JsonConverter<StringBool>))]
public partial class StringBool(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly StringBool True = new("true");
    public bool IsTrue => True.Equals(Value);

    public static readonly StringBool False = new("false");
    public bool IsFalse => False.Equals(Value);
}