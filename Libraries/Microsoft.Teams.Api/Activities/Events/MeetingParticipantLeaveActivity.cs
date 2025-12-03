// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Events;

public partial class Name : StringEnum
{
    public static readonly Name MeetingParticipantLeave = new("application/vnd.microsoft.meetingParticipantLeave");
    public bool IsMeetingParticipantLeave => MeetingParticipantLeave.Equals(Value);
}

public class MeetingParticipantLeaveActivity() : EventActivity(Name.MeetingParticipantLeave)
{
    /// <summary>
    /// A value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(32)]
    public required MeetingParticipantLeaveActivityValue Value { get; set; }
}

/// <summary>
/// A value that is associated with the activity.
/// </summary>
public class MeetingParticipantLeaveActivityValue
{
    /// <summary>
    /// The participants info.
    /// </summary>
    [JsonPropertyName("members")]
    [JsonPropertyOrder(0)]
    public required IList<Member> Members { get; set; }

    public class Member
    {
        /// <summary>
        /// The participant account.
        /// </summary>
        [JsonPropertyName("user")]
        [JsonPropertyOrder(0)]
        public required TeamsChannelAccount User { get; set; }

        /// <summary>
        /// The participants info.
        /// </summary>
        [JsonPropertyName("meeting")]
        [JsonPropertyOrder(1)]
        public required Meeting Meeting { get; set; }
    }

    public class Meeting
    {
        /// <summary>
        /// The user in meeting indicator.
        /// </summary>
        [JsonPropertyName("inMeeting")]
        [JsonPropertyOrder(0)]
        public required bool InMeeting { get; set; }

        [JsonPropertyName("role")]
        [JsonPropertyOrder(1)]
        public required Role Role { get; set; }
    }
}