// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace Microsoft.Teams.Bot.Compat;

internal static class CompatTeamsInfoModels
{
    /// <summary>
    /// Gets the TeamsMeetingInfo object from the current activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The current activity's meeting information, or null.</returns>
    public static TeamsMeetingInfo? TeamsGetMeetingInfo(this IActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        TeamsChannelData channelData = activity.GetChannelData<Microsoft.Bot.Schema.Teams.TeamsChannelData>();
        return channelData?.Meeting;
    }

    internal sealed class SendMessageToUsersRequest
    {
        /// <summary>
        /// Gets or sets the list of members.
        /// </summary>
        [JsonPropertyName("members")]
        public IList<TeamMember>? Members { get; set; }

        /// <summary>
        /// Gets or sets the activity to send.
        /// </summary>
        [JsonPropertyName("activity")]
        public object? Activity { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
    }

    internal sealed class SendMessageToTeamRequest
    {
        /// <summary>
        /// Gets or sets the activity to send.
        /// </summary>
        [JsonPropertyName("activity")]
        public object? Activity { get; set; }

        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        [JsonPropertyName("teamId")]
        public string? TeamId { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
    }

    /// <summary>
    /// Request body for sending a message to all users in a tenant.
    /// </summary>
    internal sealed class SendMessageToTenantRequest
    {
        /// <summary>
        /// Gets or sets the activity to send.
        /// </summary>
        [JsonPropertyName("activity")]
        public object? Activity { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
    }
}
