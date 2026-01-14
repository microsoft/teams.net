// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Represents a list of channels in a team.
/// </summary>
public class ChannelList
{
    /// <summary>
    /// Gets or sets the list of channel conversations.
    /// </summary>
    [JsonPropertyName("conversations")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<TeamsChannel>? Conversations { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents detailed information about a team.
/// </summary>
public class TeamDetails
{
    /// <summary>
    /// Gets or sets the unique identifier of the team.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the team.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the Azure Active Directory group ID associated with the team.
    /// </summary>
    [JsonPropertyName("aadGroupId")]
    public string? AadGroupId { get; set; }

    /// <summary>
    /// Gets or sets the number of channels in the team.
    /// </summary>
    [JsonPropertyName("channelCount")]
    public int? ChannelCount { get; set; }

    /// <summary>
    /// Gets or sets the number of members in the team.
    /// </summary>
    [JsonPropertyName("memberCount")]
    public int? MemberCount { get; set; }

    /// <summary>
    /// Gets or sets the type of the team. Valid values are standard, sharedChannel and privateChannel.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// Represents information about a meeting.
/// </summary>
public class MeetingInfo
{
    /// <summary>
    /// Gets or sets the unique identifier of the meeting.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the details of the meeting.
    /// </summary>
    [JsonPropertyName("details")]
    public MeetingDetails? Details { get; set; }

    /// <summary>
    /// Gets or sets the conversation associated with the meeting.
    /// </summary>
    [JsonPropertyName("conversation")]
    public ConversationAccount? Conversation { get; set; }

    /// <summary>
    /// Gets or sets the organizer of the meeting.
    /// </summary>
    [JsonPropertyName("organizer")]
    public ConversationAccount? Organizer { get; set; }
}

/// <summary>
/// Represents detailed information about a meeting.
/// </summary>
public class MeetingDetails
{
    /// <summary>
    /// Gets or sets the unique identifier of the meeting.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the Microsoft Graph resource ID of the meeting.
    /// </summary>
    [JsonPropertyName("msGraphResourceId")]
    public string? MsGraphResourceId { get; set; }

    /// <summary>
    /// Gets or sets the scheduled start time of the meeting.
    /// </summary>
    [JsonPropertyName("scheduledStartTime")]
    public DateTimeOffset? ScheduledStartTime { get; set; }

    /// <summary>
    /// Gets or sets the scheduled end time of the meeting.
    /// </summary>
    [JsonPropertyName("scheduledEndTime")]
    public DateTimeOffset? ScheduledEndTime { get; set; }

    /// <summary>
    /// Gets or sets the join URL of the meeting.
    /// </summary>
    [JsonPropertyName("joinUrl")]
    public Uri? JoinUrl { get; set; }

    /// <summary>
    /// Gets or sets the title of the meeting.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the type of the meeting.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>
/// Represents a meeting participant with their details.
/// </summary>
public class MeetingParticipant
{
    /// <summary>
    /// Gets or sets the user information.
    /// </summary>
    [JsonPropertyName("user")]
    public ConversationAccount? User { get; set; }

    /// <summary>
    /// Gets or sets the meeting information.
    /// </summary>
    [JsonPropertyName("meeting")]
    public MeetingParticipantInfo? Meeting { get; set; }

    /// <summary>
    /// Gets or sets the conversation information.
    /// </summary>
    [JsonPropertyName("conversation")]
    public ConversationAccount? Conversation { get; set; }
}

/// <summary>
/// Represents meeting-specific participant information.
/// </summary>
public class MeetingParticipantInfo
{
    /// <summary>
    /// Gets or sets the role of the participant in the meeting.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the participant is in the meeting.
    /// </summary>
    [JsonPropertyName("inMeeting")]
    public bool? InMeeting { get; set; }
}

/// <summary>
/// Base class for meeting notifications.
/// </summary>
public abstract class MeetingNotificationBase
{
    /// <summary>
    /// Gets or sets the type of the notification.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Represents a targeted meeting notification.
/// </summary>
public class TargetedMeetingNotification : MeetingNotificationBase
{
    /// <inheritdoc/>
    [JsonPropertyName("type")]
    public override string Type => "targetedMeetingNotification";

    /// <summary>
    /// Gets or sets the value of the notification.
    /// </summary>
    [JsonPropertyName("value")]
    public TargetedMeetingNotificationValue? Value { get; set; }
}

/// <summary>
/// Represents the value of a targeted meeting notification.
/// </summary>
public class TargetedMeetingNotificationValue
{
    /// <summary>
    /// Gets or sets the list of recipients for the notification.
    /// </summary>
    [JsonPropertyName("recipients")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<string>? Recipients { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets the surface configurations for the notification.
    /// </summary>
    [JsonPropertyName("surfaces")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<MeetingNotificationSurface>? Surfaces { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents a surface for meeting notifications.
/// </summary>
public class MeetingNotificationSurface
{
    /// <summary>
    /// Gets or sets the surface type (e.g., "meetingStage").
    /// </summary>
    [JsonPropertyName("surface")]
    public string? Surface { get; set; }

    /// <summary>
    /// Gets or sets the content type of the notification.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the content of the notification.
    /// </summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

/// <summary>
/// Response from sending a meeting notification.
/// </summary>
public class MeetingNotificationResponse
{
    /// <summary>
    /// Gets or sets the list of recipients for whom the notification failed.
    /// </summary>
    [JsonPropertyName("recipientsFailureInfo")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<MeetingNotificationRecipientFailureInfo>? RecipientsFailureInfo { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Information about a failed notification recipient.
/// </summary>
public class MeetingNotificationRecipientFailureInfo
{
    /// <summary>
    /// Gets or sets the recipient ID.
    /// </summary>
    [JsonPropertyName("recipientMri")]
    public string? RecipientMri { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the failure reason.
    /// </summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; set; }
}

/// <summary>
/// Represents a team member for batch operations.
/// </summary>
public class TeamMember
{
    /// <summary>
    /// Creates a new instance of the <see cref="TeamMember"/> class.
    /// </summary>
    public TeamMember()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TeamMember"/> class with the specified ID.
    /// </summary>
    /// <param name="id">The member ID.</param>
    public TeamMember(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets or sets the member ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Represents the state of a batch operation.
/// </summary>
public class BatchOperationState
{
    /// <summary>
    /// Gets or sets the state of the operation.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the status map containing the count of different statuses.
    /// </summary>
    [JsonPropertyName("statusMap")]
    public BatchOperationStatusMap? StatusMap { get; set; }

    /// <summary>
    /// Gets or sets the retry after date time.
    /// </summary>
    [JsonPropertyName("retryAfter")]
    public DateTimeOffset? RetryAfter { get; set; }

    /// <summary>
    /// Gets or sets the total entries count.
    /// </summary>
    [JsonPropertyName("totalEntriesCount")]
    public int? TotalEntriesCount { get; set; }
}

/// <summary>
/// Represents the status map for a batch operation.
/// </summary>
public class BatchOperationStatusMap
{
    /// <summary>
    /// Gets or sets the count of successful entries.
    /// </summary>
    [JsonPropertyName("success")]
    public int? Success { get; set; }

    /// <summary>
    /// Gets or sets the count of failed entries.
    /// </summary>
    [JsonPropertyName("failed")]
    public int? Failed { get; set; }

    /// <summary>
    /// Gets or sets the count of throttled entries.
    /// </summary>
    [JsonPropertyName("throttled")]
    public int? Throttled { get; set; }

    /// <summary>
    /// Gets or sets the count of pending entries.
    /// </summary>
    [JsonPropertyName("pending")]
    public int? Pending { get; set; }
}

/// <summary>
/// Response containing failed entries from a batch operation.
/// </summary>
public class BatchFailedEntriesResponse
{
    /// <summary>
    /// Gets or sets the continuation token for paging.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Gets or sets the list of failed entries.
    /// </summary>
    [JsonPropertyName("failedEntries")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<BatchFailedEntry>? FailedEntries { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents a failed entry in a batch operation.
/// </summary>
public class BatchFailedEntry
{
    /// <summary>
    /// Gets or sets the ID of the failed entry.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Request body for sending a message to a list of users.
/// </summary>
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

/// <summary>
/// Request body for sending a message to all users in a team.
/// </summary>
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
