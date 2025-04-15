using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Meetings;

/// <summary>
/// The details of a Meeting
/// </summary>
public class MeetingDetails
{
    /// <summary>
    /// The meeting's Id, encoded as a BASE64 string.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The meeting's type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public required string Type { get; set; }

    /// <summary>
    /// The URL used to join the meeting.
    /// </summary>
    [JsonPropertyName("joinUrl")]
    [JsonPropertyOrder(2)]
    public required string JoinUrl { get; set; }

    /// <summary>
    /// The title of the meeting.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(3)]
    public required string Title { get; set; }

    /// <summary>
    /// The MsGraphResourceId, used specifically for MS Graph API calls.
    /// </summary>
    [JsonPropertyName("msGraphResourceId")]
    [JsonPropertyOrder(4)]
    public required string MSGraphResourceId { get; set; }

    /// <summary>
    /// The meeting's scheduled start time, in UTC.
    /// </summary>
    [JsonPropertyName("scheduledStartTime")]
    [JsonPropertyOrder(5)]
    public DateTime? ScheduledStartTime { get; set; }

    /// <summary>
    /// The meeting's scheduled end time, in UTC.
    /// </summary>
    [JsonPropertyName("scheduledEndTime")]
    [JsonPropertyOrder(6)]
    public DateTime? ScheduledEndTime { get; set; }
}