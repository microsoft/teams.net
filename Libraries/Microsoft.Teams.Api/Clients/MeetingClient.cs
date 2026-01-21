// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Meetings;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class MeetingClient : Client
{
    public readonly string ServiceUrl;

    public MeetingClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public MeetingClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public MeetingClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public MeetingClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public async Task<Meeting> GetByIdAsync(string id)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v1/meetings/{id}");
        var response = await _http.SendAsync<Meeting>(request, _cancellationToken);
        return response.Body;
    }

    public async Task<MeetingParticipant> GetParticipantAsync(string meetingId, string id, string tenantId)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v1/meetings/{Uri.EscapeDataString(meetingId)}/participants/{Uri.EscapeDataString(id)}?tenantId={Uri.EscapeDataString(tenantId)}");
        var response = await _http.SendAsync<MeetingParticipant>(request, _cancellationToken);
        return response.Body;
    }
}

/// <summary>
/// Meeting participant information
/// </summary>
public class MeetingParticipant
{
    /// <summary>
    /// The participant's user information
    /// </summary>
    [JsonPropertyName("user")]
    public Account? User { get; set; }

    /// <summary>
    /// Gets or sets information about the associated meeting.
    /// </summary>
    [JsonPropertyName("meeting")]
    public MeetingInfo? Meeting { get; set; }

    /// <summary>
    /// Gets or sets the conversation associated with this object.
    /// </summary>
    [JsonPropertyName("conversation")]
    public Conversation? Conversation { get; set; }
}

/// <summary>
/// Represents information about a participant's role and meeting status within a meeting context.
/// </summary>
public class MeetingInfo
{
    /// <summary>
    /// Gets or sets the role associated with the entity.
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is currently in a meeting.
    /// </summary>
    [JsonPropertyName("inMeeting")]
    public bool? InMeeting { get; set; }
}