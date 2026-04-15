// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for reaction operations.
/// Delegates to <see cref="CoreConversationClient"/>.
/// </summary>
public class ReactionClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultIdentity;

    internal ReactionClient(CoreConversationClient client, Uri serviceUrl, AgenticIdentity? defaultIdentity = null)
    {
        _client = client;
        _serviceUrl = serviceUrl;
        _defaultIdentity = defaultIdentity;
    }

    public Task AddAsync(string conversationId, string activityId, string reactionType, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.AddReactionAsync(conversationId, activityId, reactionType, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(string conversationId, string activityId, string reactionType, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteReactionAsync(conversationId, activityId, reactionType, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }
}
