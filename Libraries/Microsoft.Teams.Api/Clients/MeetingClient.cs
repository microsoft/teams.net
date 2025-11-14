// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Meetings;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class MeetingClient : Client
{
    public readonly string ServiceUrl;
    private readonly ApiClientOptions _apiClientOptions;

    public MeetingClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public MeetingClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public MeetingClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public MeetingClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
    }

    public MeetingClient(string serviceUrl, IHttpClient client, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
    }

    public MeetingClient(string serviceUrl, IHttpClientOptions options, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
    }

    public MeetingClient(string serviceUrl, IHttpClientFactory factory, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
    }

    public async Task<Meeting> GetByIdAsync(string id)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v1/meetings/{id}");
        var response = await _http.SendAsync<Meeting>(request, _cancellationToken);
        return response.Body;
    }

    public async Task<MeetingParticipant> GetParticipantAsync(string meetingId, string id)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v1/meetings/{meetingId}/participants/{id}");
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
    /// Unique identifier for the participant
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// The participant's user information
    /// </summary>
    [JsonPropertyName("user")]
    [JsonPropertyOrder(1)]
    public Account? User { get; set; }

    /// <summary>
    /// The participant's role in the meeting
    /// </summary>
    [JsonPropertyName("role")]
    [JsonPropertyOrder(2)]
    public string? Role { get; set; }

    /// <summary>
    /// Whether the participant is an organizer
    /// </summary>
    [JsonPropertyName("isOrganizer")]
    [JsonPropertyOrder(3)]
    public bool IsOrganizer { get; set; }

    /// <summary>
    /// The time when the participant joined the meeting
    /// </summary>
    [JsonPropertyName("joinTime")]
    [JsonPropertyOrder(4)]
    public DateTime? JoinTime { get; set; }
}