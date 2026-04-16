// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Http;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for creating, updating, and deleting activities in a conversation.
/// </summary>
public class ActivityClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;

    internal ActivityClient(string serviceUrl, BotHttpClient http)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
    }

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    public async Task<SendActivityResponse?> CreateAsync(string conversationId, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities";
        string body = activity.ToJson();
        return await _http.SendAsync<SendActivityResponse>(HttpMethod.Post, url, body, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    public async Task<UpdateActivityResponse?> UpdateAsync(string conversationId, string id, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(id)}";
        string body = activity.ToJson();
        return await _http.SendAsync<UpdateActivityResponse>(HttpMethod.Put, url, body, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    public async Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ReplyToId = id;
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(id)}";
        string body = activity.ToJson();
        return await _http.SendAsync<SendActivityResponse>(HttpMethod.Post, url, body, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete an activity from a conversation.
    /// </summary>
    public async Task DeleteAsync(string conversationId, string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(id)}";
        await _http.SendAsync(HttpMethod.Delete, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    public async Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities?isTargetedActivity=true";
        string body = activity.ToJson();
        return await _http.SendAsync<SendActivityResponse>(HttpMethod.Post, url, body, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    public async Task<UpdateActivityResponse?> UpdateTargetedAsync(string conversationId, string id, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(id)}?isTargetedActivity=true";
        string body = activity.ToJson();
        return await _http.SendAsync<UpdateActivityResponse>(HttpMethod.Put, url, body, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    public async Task DeleteTargetedAsync(string conversationId, string id, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(id)}?isTargetedActivity=true";
        await _http.SendAsync(HttpMethod.Delete, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }
}
