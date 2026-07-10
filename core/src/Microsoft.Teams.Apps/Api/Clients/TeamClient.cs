// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for retrieving team information and channels.
/// </summary>
public class TeamClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;
    private readonly AgenticIdentity? _defaultAgenticIdentity;

    internal TeamClient(string serviceUrl, BotHttpClient http, AgenticIdentity? defaultAgenticIdentity = null)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
        _defaultAgenticIdentity = defaultAgenticIdentity;
    }

    /// <summary>
    /// Get a team by its ID.
    /// </summary>
    public async Task<Team?> GetByIdAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}";
        return await _http.SendAsync<Team>(HttpMethod.Get, url, body: null, options: new BotRequestOptions { RequestContext = BotRequestContext.FromAgenticIdentity(agenticIdentity ?? _defaultAgenticIdentity) }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the channels (conversations) for a team.
    /// </summary>
    public async Task<List<TeamsChannel>?> GetConversationsAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}/conversations";
        ConversationListResponse? response = await _http.SendAsync<ConversationListResponse>(HttpMethod.Get, url, body: null, options: new BotRequestOptions { RequestContext = BotRequestContext.FromAgenticIdentity(agenticIdentity ?? _defaultAgenticIdentity) }, cancellationToken).ConfigureAwait(false);
        return response?.Conversations;
    }

    private sealed class ConversationListResponse
    {
        [JsonPropertyName("conversations")]
        public List<TeamsChannel>? Conversations { get; set; }
    }
}
