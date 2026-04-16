// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Http;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for retrieving team information and channels.
/// </summary>
public class TeamClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;

    internal TeamClient(string serviceUrl, BotHttpClient http)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
    }

    /// <summary>
    /// Get a team by its ID.
    /// </summary>
    public async Task<Team?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}";
        return await _http.SendAsync<Team>(HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the channels (conversations) for a team.
    /// </summary>
    public async Task<List<TeamsChannel>?> GetConversationsAsync(string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}/conversations";
        return await _http.SendAsync<List<TeamsChannel>>(HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }
}
