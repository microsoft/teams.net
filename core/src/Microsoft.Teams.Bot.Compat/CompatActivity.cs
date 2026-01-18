// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

using Microsoft.Bot.Builder.Integration.AspNet.Core.Handlers;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Teams.Bot.Apps.Schema;
using Newtonsoft.Json;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Extension methods for converting between Bot Framework Activity and CoreActivity/TeamsActivity.
/// </summary>
public static class CompatActivity
{
    /// <summary>
    /// Converts a CoreActivity to a Bot Framework Activity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static Activity ToCompatActivity(this CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        using JsonTextReader reader = new(new StringReader(activity.ToJson()));
        return BotMessageHandlerBase.BotMessageSerializer.Deserialize<Activity>(reader)!;
    }

    /// <summary>
    /// Converts a Bot Framework Activity to a TeamsActivity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static TeamsActivity FromCompatActivity(this Activity activity)
    {
        StringBuilder sb = new();
        using StringWriter stringWriter = new(sb);
        using JsonTextWriter json = new(stringWriter);
        BotMessageHandlerBase.BotMessageSerializer.Serialize(json, activity);
        string jsonString = sb.ToString();
        CoreActivity coreActivity = CoreActivity.FromJsonString(jsonString);
        return TeamsActivity.FromActivity(coreActivity);
    }


    /// <summary>
    /// Converts a ConversationAccount to a ChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.ChannelAccount ToCompatChannelAccount(this Microsoft.Teams.Bot.Core.Schema.ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Microsoft.Bot.Schema.ChannelAccount channelAccount;
        if (account is TeamsConversationAccount tae)
        {
            channelAccount = new()
            {
                Id = account.Id,
                Name = account.Name,
                AadObjectId = tae.AadObjectId
            };
        }
        else
        {
            channelAccount = new()
            {
                Id = account.Id,
                Name = account.Name
            };
        }

        if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
        {
            channelAccount.AadObjectId = aadObjectId?.ToString();
        }

        if (account.Properties.TryGetValue("userRole", out object? userRole))
        {
            channelAccount.Role = userRole?.ToString();
        }

        if (account.Properties.TryGetValue("userPrincipalName", out object? userPrincipalName))
        {
            channelAccount.Properties.Add("userPrincipalName", userPrincipalName?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("givenName", out object? givenName))
        {
            channelAccount.Properties.Add("givenName", givenName?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("surname", out object? surname))
        {
            channelAccount.Properties.Add("surname", surname?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("email", out object? email))
        {
            channelAccount.Properties.Add("email", email?.ToString() ?? string.Empty);
        }

        if (account.Properties.TryGetValue("tenantId", out object? tenantId))
        {
            channelAccount.Properties.Add("tenantId", tenantId?.ToString() ?? string.Empty);
        }

        return channelAccount;
    }

    /// <summary>
    /// Converts a TeamsConversationAccount to a TeamsChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsChannelAccount ToCompatTeamsChannelAccount(this Microsoft.Teams.Bot.Apps.Schema.TeamsConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new Microsoft.Bot.Schema.Teams.TeamsChannelAccount
        {
            Id = account.Id,
            Name = account.Name,
            AadObjectId = account.AadObjectId,
            Email = account.Email,
            GivenName = account.GivenName,
            Surname = account.Surname,
            UserPrincipalName = account.UserPrincipalName,
            UserRole = account.UserRole,
            TenantId = account.TenantId
        };
    }

    /// <summary>
    /// Converts a Core MeetingInfo to a Bot Framework MeetingInfo.
    /// </summary>
    /// <param name="meetingInfo"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.MeetingInfo ToCompatMeetingInfo(this Microsoft.Teams.Bot.Apps.MeetingInfo meetingInfo)
    {
        ArgumentNullException.ThrowIfNull(meetingInfo);

        return new Microsoft.Bot.Schema.Teams.MeetingInfo
        {
            Details = meetingInfo.Details != null ? new Microsoft.Bot.Schema.Teams.MeetingDetails
            {
                Id = meetingInfo.Details.Id,
                MsGraphResourceId = meetingInfo.Details.MsGraphResourceId,
                ScheduledStartTime = meetingInfo.Details.ScheduledStartTime?.DateTime,
                ScheduledEndTime = meetingInfo.Details.ScheduledEndTime?.DateTime,
                JoinUrl = meetingInfo.Details.JoinUrl,
                Title = meetingInfo.Details.Title,
                Type = meetingInfo.Details.Type
            } : null,
            Conversation = meetingInfo.Conversation != null ? new Microsoft.Bot.Schema.ConversationAccount
            {
                Id = meetingInfo.Conversation.Id,
                Name = meetingInfo.Conversation.Name
            } : null,
            Organizer = meetingInfo.Organizer != null ? meetingInfo.Organizer.ToCompatTeamsChannelAccount() : null
        };
    }

    /// <summary>
    /// Converts a Core MeetingParticipant to a Bot Framework TeamsMeetingParticipant.
    /// </summary>
    /// <param name="participant"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsMeetingParticipant ToCompatTeamsMeetingParticipant(this Microsoft.Teams.Bot.Apps.MeetingParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);

        return new Microsoft.Bot.Schema.Teams.TeamsMeetingParticipant
        {
            User = participant.User != null ? participant.User.ToCompatTeamsChannelAccount() : null,
            Meeting = participant.Meeting != null ? new Microsoft.Bot.Schema.Teams.MeetingParticipantInfo
            {
                Role = participant.Meeting.Role,
                InMeeting = participant.Meeting.InMeeting
            } : null,
            Conversation = participant.Conversation != null ? new Microsoft.Bot.Schema.ConversationAccount
            {
                Id = participant.Conversation.Id
            } : null
        };
    }

    /// <summary>
    /// Converts a Core TeamsChannel to a Bot Framework ChannelInfo.
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.ChannelInfo ToCompatChannelInfo(this Microsoft.Teams.Bot.Apps.Schema.TeamsChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        return new Microsoft.Bot.Schema.Teams.ChannelInfo
        {
            Id = channel.Id,
            Name = channel.Name
        };
    }

    /// <summary>
    /// Converts a Core PagedMembersResult to a Bot Framework TeamsPagedMembersResult.
    /// </summary>
    /// <param name="pagedMembers"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsPagedMembersResult ToCompatTeamsPagedMembersResult(this Microsoft.Teams.Bot.Core.PagedMembersResult pagedMembers)
    {
        ArgumentNullException.ThrowIfNull(pagedMembers);

        return new Microsoft.Bot.Schema.Teams.TeamsPagedMembersResult
        {
            ContinuationToken = pagedMembers.ContinuationToken,
            Members = pagedMembers.Members?.Select(m => m.ToCompatTeamsChannelAccount()).ToList()
        };
    }

    /// <summary>
    /// Converts a ConversationAccount to a TeamsChannelAccount.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public static Microsoft.Bot.Schema.Teams.TeamsChannelAccount ToCompatTeamsChannelAccount(this Microsoft.Teams.Bot.Core.Schema.ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var teamsChannelAccount = new Microsoft.Bot.Schema.Teams.TeamsChannelAccount
        {
            Id = account.Id,
            Name = account.Name
        };

        // Extract properties from Properties dictionary
        if (account.Properties.TryGetValue("aadObjectId", out object? aadObjectId))
        {
            teamsChannelAccount.AadObjectId = aadObjectId?.ToString();
        }

        if (account.Properties.TryGetValue("userPrincipalName", out object? userPrincipalName))
        {
            teamsChannelAccount.UserPrincipalName = userPrincipalName?.ToString();
        }

        if (account.Properties.TryGetValue("givenName", out object? givenName))
        {
            teamsChannelAccount.GivenName = givenName?.ToString();
        }

        if (account.Properties.TryGetValue("surname", out object? surname))
        {
            teamsChannelAccount.Surname = surname?.ToString();
        }

        if (account.Properties.TryGetValue("email", out object? email))
        {
            teamsChannelAccount.Email = email?.ToString();
        }

        if (account.Properties.TryGetValue("tenantId", out object? tenantId))
        {
            teamsChannelAccount.Properties.Add("tenantId", tenantId?.ToString() ?? string.Empty);
        }

        return teamsChannelAccount;
    }

    /// <summary>
    /// Gets the TeamInfo object from the current activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The current activity's team's information, or null.</returns>
    public static TeamInfo? TeamsGetTeamInfo(this IActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        var channelData = activity.GetChannelData<Microsoft.Bot.Schema.Teams.TeamsChannelData>();
        return channelData?.Team;
    }

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
