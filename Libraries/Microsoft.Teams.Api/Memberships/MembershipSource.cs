using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Memberships;

/// <summary>
/// Represents the source of a membership
/// </summary>
public class MembershipSource
{
    /// <summary>
    /// The unique identifier for the membership source
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The type of roster the user is a member of
    /// </summary>
    [JsonPropertyName("sourceType")]
    [JsonPropertyOrder(1)]
    public required MembershipSourceType SourceType { get; set; }

    /// <summary>
    /// The users relationship to the current channel
    /// </summary>
    [JsonPropertyName("membershipType")]
    [JsonPropertyOrder(2)]
    public required MembershipType MembershipType { get; set; }

    /// <summary>
    /// The tenant Id of the user
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonPropertyOrder(3)]
    public required string TenantId { get; set; }

    /// <summary>
    /// The group Id of the team associated with this membership source
    /// </summary>
    [JsonPropertyName("teamGroupId")]
    [JsonPropertyOrder(4)]
    public required string TeamGroupId { get; set; }
}