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

    internal TeamClient(string serviceUrl, BotHttpClient http)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
    }

    /// <summary>
    /// Get a team by its ID.
    /// </summary>
    public async Task<Team?> GetByIdAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}";
        return await _http.SendAsync<Team>(HttpMethod.Get, url, body: null, options: CreateRequestOptions(agenticIdentity), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the channels (conversations) for a team.
    /// </summary>
    public async Task<List<TeamsChannel>?> GetConversationsAsync(string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/teams/{Uri.EscapeDataString(id)}/conversations";
        ConversationListResponse? response = await _http.SendAsync<ConversationListResponse>(HttpMethod.Get, url, body: null, options: CreateRequestOptions(agenticIdentity), cancellationToken).ConfigureAwait(false);
        return response?.Conversations;
    }

    private static BotRequestOptions? CreateRequestOptions(AgenticIdentity? agenticIdentity) =>
        agenticIdentity is null ? null : new() { RequestContext = BotRequestContext.FromAgenticIdentity(agenticIdentity) };

    private sealed class ConversationListResponse
    {
        [JsonPropertyName("conversations")]
        public List<TeamsChannel>? Conversations { get; set; }
    }
}
