// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        var channelData = activity.GetChannelData<Microsoft.Bot.Schema.Teams.TeamsChannelData>();
        return channelData?.Meeting;
    }

    /// <summary>
    /// Converts a Core BatchOperationState to a Bot Framework BatchOperationState using JSON round-trip.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.BatchOperationState ToCompatBatchOperationState(this Microsoft.Teams.Bot.Apps.BatchOperationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var json = System.Text.Json.JsonSerializer.Serialize(state);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Microsoft.Bot.Schema.Teams.BatchOperationState>(json)!;
    }

    /// <summary>
    /// Converts a Core BatchFailedEntriesResponse to a Bot Framework BatchFailedEntriesResponse using JSON round-trip.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.BatchFailedEntriesResponse ToCompatBatchFailedEntriesResponse(this Microsoft.Teams.Bot.Apps.BatchFailedEntriesResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var json = System.Text.Json.JsonSerializer.Serialize(response);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Microsoft.Bot.Schema.Teams.BatchFailedEntriesResponse>(json)!;
    }

    /// <summary>
    /// Converts a Core TeamDetails to a Bot Framework TeamDetails using JSON round-trip.
    /// </summary>
    /// <param name="teamDetails"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamDetails ToCompatTeamDetails(this Microsoft.Teams.Bot.Apps.TeamDetails teamDetails)
    {
        ArgumentNullException.ThrowIfNull(teamDetails);

        var json = System.Text.Json.JsonSerializer.Serialize(teamDetails);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Microsoft.Bot.Schema.Teams.TeamDetails>(json)!;
    }

    /// <summary>
    /// Converts a Core MeetingNotificationResponse to a Bot Framework MeetingNotificationResponse using JSON round-trip.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.MeetingNotificationResponse ToCompatMeetingNotificationResponse(this Microsoft.Teams.Bot.Apps.MeetingNotificationResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var json = System.Text.Json.JsonSerializer.Serialize(response);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Microsoft.Bot.Schema.Teams.MeetingNotificationResponse>(json)!;
    }

    /// <summary>
    /// Converts a Bot Framework TeamMember to a Core TeamMember using JSON round-trip.
    /// </summary>
    /// <param name="teamMember"></param>
    /// <returns></returns>
    public static Microsoft.Teams.Bot.Apps.TeamMember FromCompatTeamMember(this Microsoft.Bot.Schema.Teams.TeamMember teamMember)
    {
        ArgumentNullException.ThrowIfNull(teamMember);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(teamMember);
        return System.Text.Json.JsonSerializer.Deserialize<Microsoft.Teams.Bot.Apps.TeamMember>(json)!;
    }
}
