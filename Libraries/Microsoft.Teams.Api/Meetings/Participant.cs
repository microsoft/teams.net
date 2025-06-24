// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Meetings;

/// <summary>
/// Teams meeting participant detailing user Azure Active Directory details.
/// </summary>
public class Participant
{
    /// <summary>
    /// Meeting role of the user.
    /// </summary>
    [JsonPropertyName("role")]
    [JsonPropertyOrder(0)]
    public Role? User { get; set; }

    /// <summary>
    /// Indicates if the participant is in the meeting.
    /// </summary>
    [JsonPropertyName("inMeeting")]
    [JsonPropertyOrder(1)]
    public bool? InMeeting { get; set; }
}