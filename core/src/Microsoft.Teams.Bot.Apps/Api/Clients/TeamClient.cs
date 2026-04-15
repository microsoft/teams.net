// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for team operations.
/// Delegates to <see cref="TeamsApiClient"/>.
/// </summary>
public class TeamClient
{
    private readonly TeamsApiClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultIdentity;

    internal TeamClient(TeamsApiClient client, Uri serviceUrl, AgenticIdentity? defaultIdentity = null)
    {
        _client = client;
        _serviceUrl = serviceUrl;
        _defaultIdentity = defaultIdentity;
    }

    public Task<TeamDetails> GetByIdAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.FetchTeamDetailsAsync(id, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public Task<ChannelList> GetConversationsAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.FetchChannelListAsync(id, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }
}
