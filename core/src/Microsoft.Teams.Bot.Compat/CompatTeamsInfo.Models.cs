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
    /// Converts a Core BatchOperationState to a Bot Framework BatchOperationState.
    /// </summary>
    /// <param name="state">The source state.</param>
    /// <returns>The converted Bot Framework BatchOperationState.</returns>
    public static Microsoft.Bot.Schema.Teams.BatchOperationState ToCompatBatchOperationState(this Microsoft.Teams.Bot.Apps.BatchOperationState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var result = new Microsoft.Bot.Schema.Teams.BatchOperationState
        {
            State = state.State,
            RetryAfter = state.RetryAfter?.DateTime,
            TotalEntriesCount = state.TotalEntriesCount ?? 0
        };

        // StatusMap in Bot Framework SDK is IDictionary<int, int> (read-only property)
        // Map from BatchOperationStatusMap to the dictionary format
        if (state.StatusMap != null)
        {
            if (state.StatusMap.Success.HasValue)
            {
                result.StatusMap[0] = state.StatusMap.Success.Value;
            }

            if (state.StatusMap.Failed.HasValue)
            {
                result.StatusMap[1] = state.StatusMap.Failed.Value;
            }

            if (state.StatusMap.Throttled.HasValue)
            {
                result.StatusMap[2] = state.StatusMap.Throttled.Value;
            }

            if (state.StatusMap.Pending.HasValue)
            {
                result.StatusMap[3] = state.StatusMap.Pending.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a Core BatchFailedEntriesResponse to a Bot Framework BatchFailedEntriesResponse.
    /// </summary>
    /// <param name="response">The source response.</param>
    /// <returns>The converted Bot Framework BatchFailedEntriesResponse.</returns>
    public static Microsoft.Bot.Schema.Teams.BatchFailedEntriesResponse ToCompatBatchFailedEntriesResponse(this Microsoft.Teams.Bot.Apps.BatchFailedEntriesResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var result = new Microsoft.Bot.Schema.Teams.BatchFailedEntriesResponse
        {
            ContinuationToken = response.ContinuationToken
        };

        // FailedEntries is a read-only property with private setter, populate via the collection
        if (response.FailedEntries != null)
        {
            foreach (var entry in response.FailedEntries)
            {
                result.FailedEntries.Add(entry.ToCompatBatchFailedEntry());
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a Core BatchFailedEntry to a Bot Framework BatchFailedEntry.
    /// </summary>
    /// <param name="entry">The source entry.</param>
    /// <returns>The converted Bot Framework BatchFailedEntry.</returns>
    public static Microsoft.Bot.Schema.Teams.BatchFailedEntry ToCompatBatchFailedEntry(this Microsoft.Teams.Bot.Apps.BatchFailedEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new Microsoft.Bot.Schema.Teams.BatchFailedEntry
        {
            EntryId = entry.Id,
            Error = entry.Error
        };
    }

    /// <summary>
    /// Converts a Core TeamDetails to a Bot Framework TeamDetails.
    /// </summary>
    /// <param name="teamDetails">The source team details.</param>
    /// <returns>The converted Bot Framework TeamDetails.</returns>
    public static Microsoft.Bot.Schema.Teams.TeamDetails ToCompatTeamDetails(this Microsoft.Teams.Bot.Apps.TeamDetails teamDetails)
    {
        ArgumentNullException.ThrowIfNull(teamDetails);

        return new Microsoft.Bot.Schema.Teams.TeamDetails
        {
            Id = teamDetails.Id,
            Name = teamDetails.Name,
            AadGroupId = teamDetails.AadGroupId,
            ChannelCount = teamDetails.ChannelCount ?? 0,
            MemberCount = teamDetails.MemberCount ?? 0,
            Type = teamDetails.Type
        };
    }

    /// <summary>
    /// Converts a Core MeetingNotificationResponse to a Bot Framework MeetingNotificationResponse.
    /// </summary>
    /// <param name="response">The source response.</param>
    /// <returns>The converted Bot Framework MeetingNotificationResponse.</returns>
    public static Microsoft.Bot.Schema.Teams.MeetingNotificationResponse ToCompatMeetingNotificationResponse(this Microsoft.Teams.Bot.Apps.MeetingNotificationResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new Microsoft.Bot.Schema.Teams.MeetingNotificationResponse
        {
            RecipientsFailureInfo = response.RecipientsFailureInfo?.Select(r => r.ToCompatMeetingNotificationRecipientFailureInfo()).ToList()
        };
    }

    /// <summary>
    /// Converts a Core MeetingNotificationRecipientFailureInfo to a Bot Framework MeetingNotificationRecipientFailureInfo.
    /// </summary>
    /// <param name="info">The source failure info.</param>
    /// <returns>The converted Bot Framework MeetingNotificationRecipientFailureInfo.</returns>
    public static Microsoft.Bot.Schema.Teams.MeetingNotificationRecipientFailureInfo ToCompatMeetingNotificationRecipientFailureInfo(this Microsoft.Teams.Bot.Apps.MeetingNotificationRecipientFailureInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return new Microsoft.Bot.Schema.Teams.MeetingNotificationRecipientFailureInfo
        {
            RecipientMri = info.RecipientMri,
            ErrorCode = info.ErrorCode,
            FailureReason = info.FailureReason
        };
    }

    /// <summary>
    /// Converts a Bot Framework TeamMember to a Core TeamMember.
    /// </summary>
    /// <param name="teamMember">The source team member.</param>
    /// <returns>The converted Core TeamMember.</returns>
    public static Microsoft.Teams.Bot.Apps.TeamMember FromCompatTeamMember(this Microsoft.Bot.Schema.Teams.TeamMember teamMember)
    {
        ArgumentNullException.ThrowIfNull(teamMember);

        return new Microsoft.Teams.Bot.Apps.TeamMember(teamMember.Id);
    }
}
