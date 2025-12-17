// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a meeting participant join event activity.
/// </summary>
public class MeetingParticipantJoinActivity : EventActivity
{
    /// <summary>
    /// Gets or sets a value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    public MeetingParticipantJoinActivityValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingParticipantJoinActivity"/> class.
    /// </summary>
    public MeetingParticipantJoinActivity() : base(EventNames.MeetingParticipantJoin)
    {
    }
}

/// <summary>
/// A value that is associated with a meeting participant join activity.
/// </summary>
public class MeetingParticipantJoinActivityValue
{
    /// <summary>
    /// Gets or sets the participants info.
    /// </summary>
    [JsonPropertyName("members")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<MeetingParticipantMember>? Members { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents a meeting participant member.
/// </summary>
public class MeetingParticipantMember
{
    /// <summary>
    /// Gets or sets the participant account.
    /// </summary>
    [JsonPropertyName("user")]
    public Account? User { get; set; }

    /// <summary>
    /// Gets or sets the participant's meeting info.
    /// </summary>
    [JsonPropertyName("meeting")]
    public MeetingParticipantMeetingInfo? Meeting { get; set; }
}

/// <summary>
/// Represents meeting participant meeting information.
/// </summary>
public class MeetingParticipantMeetingInfo
{
    /// <summary>
    /// Gets or sets a value indicating whether the user is in the meeting.
    /// </summary>
    [JsonPropertyName("inMeeting")]
    public bool InMeeting { get; set; }

    /// <summary>
    /// Gets or sets the participant's role in the meeting. See <see cref="Roles"/> for common values.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
