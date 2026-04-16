// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Http;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for managing reactions on activities in a conversation.
/// </summary>
public class ReactionClient
{
    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;

    internal ReactionClient(string serviceUrl, BotHttpClient http)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
    }

    /// <summary>
    /// Adds a reaction on an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity to react to.</param>
    /// <param name="reactionType">The reaction type (for example: "like", "heart", "laugh", etc.).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async Task AddAsync(string conversationId, string activityId, string reactionType, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}/reactions/{Uri.EscapeDataString(reactionType)}";
        await _http.SendAsync(HttpMethod.Put, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity the reaction is on.</param>
    /// <param name="reactionType">The reaction type to remove (for example: "like", "heart", "laugh", etc.).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async Task DeleteAsync(string conversationId, string activityId, string reactionType, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations/{Uri.EscapeDataString(conversationId)}/activities/{Uri.EscapeDataString(activityId)}/reactions/{Uri.EscapeDataString(reactionType)}";
        await _http.SendAsync(HttpMethod.Delete, url, body: null, options: null, cancellationToken).ConfigureAwait(false);
    }
}
