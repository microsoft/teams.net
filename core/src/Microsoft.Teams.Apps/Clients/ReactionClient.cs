// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;

namespace Microsoft.Teams.Apps.Clients;

/// <summary>
/// Client for managing reactions on activities in a conversation.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
[Obsolete("Use ConversationApiClient.AddReactionAsync and ConversationApiClient.DeleteReactionAsync instead.")]
public class ReactionClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticUser? _agenticUser;

    internal ReactionClient(Uri serviceUrl, CoreConversationClient client, AgenticUser? agenticUser = null)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        _agenticUser = agenticUser;
    }

    private BotRequestContext? AgenticUserContext => BotRequestContext.FromAgenticUser(_agenticUser);

    /// <summary>
    /// Adds a reaction on an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity to react to.</param>
    /// <param name="reactionType">The reaction type. See <see cref="ReactionTypes"/> for common values.</param>
    /// <param name="additionalHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task AddAsync(string conversationId, string activityId, string reactionType, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.AddReactionAsync(conversationId, activityId, reactionType, _serviceUrl, requestContext: AgenticUserContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity the reaction is on.</param>
    /// <param name="reactionType">The reaction type to remove. See <see cref="ReactionTypes"/> for common values.</param>
    /// <param name="additionalHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task DeleteAsync(string conversationId, string activityId, string reactionType, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteReactionAsync(conversationId, activityId, reactionType, _serviceUrl, requestContext: AgenticUserContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }
}
