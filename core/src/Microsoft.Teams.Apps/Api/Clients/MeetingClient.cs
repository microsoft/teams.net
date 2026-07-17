// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for retrieving meeting information and participants.
/// </summary>
public class MeetingClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;
    private readonly AgenticIdentity? _agenticIdentity;

    internal MeetingClient(string serviceUrl, BotHttpClient http, AgenticIdentity? agenticIdentity = null)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
        _agenticIdentity = agenticIdentity;
    }

    /// <summary>
    /// Get a meeting by its ID.
    /// </summary>
    public async Task<Meeting?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v1/meetings/{Uri.EscapeDataString(id)}";
        return await _http.SendAsync<Meeting>(HttpMethod.Get, url, body: null, options: CreateRequestOptions(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a participant in a meeting.
    /// </summary>
    public async Task<MeetingParticipant?> GetParticipantAsync(string meetingId, string id, string tenantId, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v1/meetings/{Uri.EscapeDataString(meetingId)}/participants/{Uri.EscapeDataString(id)}?tenantId={Uri.EscapeDataString(tenantId)}";
        return await _http.SendAsync<MeetingParticipant>(HttpMethod.Get, url, body: null, options: CreateRequestOptions(), cancellationToken).ConfigureAwait(false);
    }

    private BotRequestContext? AgenticContext => BotRequestContext.FromAgenticIdentity(_agenticIdentity);

    private BotRequestOptions? CreateRequestOptions() =>
        AgenticContext is { } context ? new() { RequestContext = context } : null;
}

/// <summary>
/// General information about a Teams meeting.
/// </summary>
public class Meeting
{
    /// <summary>
    /// Unique identifier representing a meeting.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The specific details of a Teams meeting.
    /// </summary>
    [JsonPropertyName("details")]
    public MeetingDetails? Details { get; set; }

    /// <summary>
    /// The conversation for the meeting.
    /// </summary>
    [JsonPropertyName("conversation")]
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// The organizer's user information.
    /// </summary>
    [JsonPropertyName("organizer")]
    public ChannelAccount? Organizer { get; set; }
}

/// <summary>
/// The specific details of a Teams meeting.
/// </summary>
public class MeetingDetails
{
    /// <summary>
    /// The meeting's Id, encoded as a BASE64 string.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The meeting's type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The URL used to join the meeting.
    /// </summary>
    [JsonPropertyName("joinUrl")]
    public Uri? JoinUrl { get; set; }

    /// <summary>
    /// The title of the meeting.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Meeting participant information.
/// </summary>
public class MeetingParticipant
{
    /// <summary>
    /// The participant's user information.
    /// </summary>
    [JsonPropertyName("user")]
    public ChannelAccount? User { get; set; }

    /// <summary>
    /// Information about the associated meeting.
    /// </summary>
    [JsonPropertyName("meeting")]
    public MeetingInfo? Meeting { get; set; }

    /// <summary>
    /// The conversation associated with this participant.
    /// </summary>
    [JsonPropertyName("conversation")]
    public Conversation? Conversation { get; set; }
}

/// <summary>
/// Represents information about a participant's role and status within a meeting.
/// </summary>
public class MeetingInfo
{
    /// <summary>
    /// The role associated with the participant.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// Whether the user is currently in a meeting.
    /// </summary>
    [JsonPropertyName("inMeeting")]
    public bool? InMeeting { get; set; }
}
