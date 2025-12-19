// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an event activity.
/// </summary>
public class EventActivity : Activity
{
    /// <summary>
    /// Gets or sets the name of the event. See <see cref="EventNames"/> for common values.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class.
    /// </summary>
    public EventActivity() : base(ActivityTypes.Event)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventActivity"/> class with the specified event name.
    /// </summary>
    /// <param name="name">The event name.</param>
    public EventActivity(string name) : base(ActivityTypes.Event)
    {
        Name = name;
    }
}

/// <summary>
/// String constants for event activity names.
/// </summary>
public static class EventNames
{
    /// <summary>
    /// Read receipt event name.
    /// </summary>
    public const string ReadReceipt = "application/vnd.microsoft.readReceipt";

    /// <summary>
    /// Meeting start event name.
    /// </summary>
    public const string MeetingStart = "application/vnd.microsoft.meetingStart";

    /// <summary>
    /// Meeting end event name.
    /// </summary>
    public const string MeetingEnd = "application/vnd.microsoft.meetingEnd";

    /// <summary>
    /// Meeting participant join event name.
    /// </summary>
    public const string MeetingParticipantJoin = "application/vnd.microsoft.meetingParticipantJoin";

    /// <summary>
    /// Meeting participant leave event name.
    /// </summary>
    public const string MeetingParticipantLeave = "application/vnd.microsoft.meetingParticipantLeave";
}
