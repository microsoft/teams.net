using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Memberships;

/// <summary>
/// The users relationship to the current channel
/// </summary>
[JsonConverter(typeof(JsonConverter<MembershipType>))]
public class MembershipType(string value) : StringEnum(value)
{
    public static readonly MembershipType Direct = new("direct");
    public bool IsDirect => Direct.Equals(Value);

    public static readonly MembershipType Transitive = new("transitive");
    public bool IsTransitive => Transitive.Equals(Value);
}