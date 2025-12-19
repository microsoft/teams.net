// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a meeting start event activity.
/// </summary>
public class MeetingStartActivity : EventActivity
{
    /// <summary>
    /// Gets or sets a value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    public MeetingStartActivityValue? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingStartActivity"/> class.
    /// </summary>
    public MeetingStartActivity() : base(EventNames.MeetingStart)
    {
    }
}

/// <summary>
/// A value that is associated with a meeting start activity.
/// </summary>
#pragma warning disable CA1056 // URI properties should not be strings
public class MeetingStartActivityValue
#pragma warning restore CA1056 // URI properties should not be strings
{
    /// <summary>
    /// Gets or sets the meeting's ID, encoded as a BASE64 string.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the meeting's type.
    /// </summary>
    [JsonPropertyName("meetingType")]
    public string? MeetingType { get; set; }

    /// <summary>
    /// Gets or sets the URL used to join the meeting.
    /// </summary>
    [JsonPropertyName("joinUrl")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Meeting activity schema uses string for JoinUrl")]
    public string? JoinUrl { get; set; }

    /// <summary>
    /// Gets or sets the title of the meeting.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for meeting start, in UTC.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }
}
