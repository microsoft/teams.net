// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Http;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for managing conversation members.
/// </summary>
public class MemberClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;

    internal MemberClient(string serviceUrl, BotHttpClient http)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
    }

    /// <summary>
    /// Get all members of a conversation.
    /// </summary>
    public async Task<List<ConversationAccount>?> GetAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/members";
        return await _http.SendAsync<List<ConversationAccount>>(HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public async Task<ConversationAccount?> GetByIdAsync(string conversationId, string memberId, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/members/{Uri.EscapeDataString(memberId)}";
        return await _http.SendAsync<ConversationAccount>(HttpMethod.Get, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove a member from a conversation.
    /// </summary>
    public async Task DeleteAsync(string conversationId, string memberId, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/members/{Uri.EscapeDataString(memberId)}";
        await _http.SendAsync(HttpMethod.Delete, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }
}
