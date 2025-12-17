// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a meeting participant leave event activity.
/// </summary>
public class MeetingParticipantLeaveActivity : EventActivity
{
    /// <summary>
    /// Gets or sets a value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    public MeetingParticipantLeaveActivityValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingParticipantLeaveActivity"/> class.
    /// </summary>
    public MeetingParticipantLeaveActivity() : base(EventNames.MeetingParticipantLeave)
    {
    }
}

/// <summary>
/// A value that is associated with a meeting participant leave activity.
/// </summary>
public class MeetingParticipantLeaveActivityValue
{
    /// <summary>
    /// Gets or sets the participants info.
    /// </summary>
    [JsonPropertyName("members")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<MeetingParticipantMember>? Members { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}
