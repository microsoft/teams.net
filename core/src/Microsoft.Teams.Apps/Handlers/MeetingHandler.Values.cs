// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Value payload for a meeting start event.
/// </summary>
public class MeetingStartValue
{
    /// <summary>The meeting's Id, encoded as a BASE64 string.</summary>
    [JsonPropertyName("Id")]
    public required string Id { get; set; }

    /// <summary>The meeting's type.</summary>
    [JsonPropertyName("MeetingType")]
    public string? MeetingType { get; internal set; } = string.Empty;

    /// <summary>The URL used to join the meeting.</summary>
    [JsonPropertyName("JoinUrl")]
    public Uri? JoinUrl { get; internal set; }

    /// <summary>The title of the meeting.</summary>
    [JsonPropertyName("Title")]
    public string? Title { get; internal set; } = string.Empty;

    /// <summary>Timestamp for meeting start, in UTC.</summary>
    [JsonPropertyName("StartTime")]
    public string? StartTime { get; internal set; }
}

/// <summary>
/// Value payload for a meeting end event.
/// </summary>
public class MeetingEndValue
{
    /// <summary>The meeting's Id, encoded as a BASE64 string.</summary>
    [JsonPropertyName("Id")]
    public required string Id { get; set; }

    /// <summary>The meeting's type.</summary>
    [JsonPropertyName("MeetingType")]
    public string? MeetingType { get; internal set; }

    /// <summary>The URL used to join the meeting.</summary>
    [JsonPropertyName("JoinUrl")]
    public Uri? JoinUrl { get; internal set; }

    /// <summary>The title of the meeting.</summary>
    [JsonPropertyName("Title")]
    public string? Title { get; internal set; }

    /// <summary>Timestamp for meeting end, in UTC.</summary>
    [JsonPropertyName("EndTime")]
    public string? EndTime { get; internal set; }
}

/// <summary>
/// Value payload for a meeting participant join event.
/// </summary>
public class MeetingParticipantJoinValue
{
    /// <summary>The list of participants who joined.</summary>
    [JsonPropertyName("members")]
    public IList<MeetingParticipantMember> Members { get; internal set; } = [];
}

/// <summary>
/// Value payload for a meeting participant leave event.
/// </summary>
public class MeetingParticipantLeaveValue
{
    /// <summary>The list of participants who left.</summary>
    [JsonPropertyName("members")]
    public IList<MeetingParticipantMember> Members { get; internal set; } = [];
}

/// <summary>
/// Represents a member in a meeting participant event.
/// </summary>
public class MeetingParticipantMember
{
    /// <summary>The participant's account.</summary>
    [JsonPropertyName("user")]
    public TeamsChannelAccount User { get; internal set; } = new();

    /// <summary>The participant's meeting info.</summary>
    [JsonPropertyName("meeting")]
    public MeetingParticipantInfo Meeting { get; internal set; } = new();
}

/// <summary>
/// Represents a participant's meeting info.
/// </summary>
public class MeetingParticipantInfo
{
    /// <summary>Whether the user is currently in the meeting.</summary>
    [JsonPropertyName("inMeeting")]
    public bool InMeeting { get; internal set; }

    /// <summary>The participant's role in the meeting.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; internal set; }
}
