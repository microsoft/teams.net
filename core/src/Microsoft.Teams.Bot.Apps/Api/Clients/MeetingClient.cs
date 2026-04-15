// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for meeting operations.
/// Delegates to <see cref="TeamsApiClient"/>.
/// </summary>
public class MeetingClient
{
    private readonly TeamsApiClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultIdentity;

    internal MeetingClient(TeamsApiClient client, Uri serviceUrl, AgenticIdentity? defaultIdentity = null)
    {
        _client = client;
        _serviceUrl = serviceUrl;
        _defaultIdentity = defaultIdentity;
    }

    public Task<MeetingInfo> GetByIdAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.FetchMeetingInfoAsync(id, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public Task<MeetingParticipant> GetParticipantAsync(string meetingId, string id, string tenantId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.FetchParticipantAsync(meetingId, id, tenantId, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }
}
