using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Memberships;

/// <summary>
/// The type of roster the user is a member of
/// </summary>
[JsonConverter(typeof(JsonConverter<MembershipSourceType>))]
public class MembershipSourceType(string value) : StringEnum(value)
{
    public static readonly MembershipSourceType Channel = new("channel");
    public bool IsChannel => Channel.Equals(Value);

    public static readonly MembershipSourceType Team = new("team");
    public bool IsTeam => Team.Equals(Value);
}