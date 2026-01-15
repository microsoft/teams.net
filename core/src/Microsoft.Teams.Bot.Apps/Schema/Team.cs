// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema
{
    /// <summary>
    /// Represents a team, including its identity, group association, and membership details.
    /// </summary>
    public class Team
    {
        /// <summary>
        /// Represents the unique identifier of the team.
        /// </summary>
        [JsonPropertyName("id")] public string? Id { get; set; }

        /// <summary>
        /// Azure Active Directory (AAD) Group ID associated with the team.
        /// </summary>
        [JsonPropertyName("aadGroupId")] public string? AadGroupId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the tenant associated with this entity.
        /// </summary>
        [JsonPropertyName("tenantId")] public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the type identifier for the object represented by this instance.
        /// </summary>
        [JsonPropertyName("type")] public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        [JsonPropertyName("name")] public string? Name { get; set; }

        /// <summary>
        /// Number of channels in the team.
        /// </summary>
        [JsonPropertyName("channelCount")] public int? ChannelCount { get; set; }

        /// <summary>
        /// Number of members in the team.
        /// </summary>
        [JsonPropertyName("memberCount")] public int? MemberCount { get; set; }
    }
}
