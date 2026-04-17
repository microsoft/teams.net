// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for managing reactions on activities in a conversation.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
public class ReactionClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;

    internal ReactionClient(Uri serviceUrl, CoreConversationClient client)
    {
        _serviceUrl = serviceUrl;
        _client = client;
    }

    /// <summary>
    /// Adds a reaction on an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity to react to.</param>
    /// <param name="reactionType">The reaction type (for example: "like", "heart", "laugh", etc.).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task AddAsync(string conversationId, string activityId, string reactionType, CancellationToken cancellationToken = default)
    {
        return _client.AddReactionAsync(conversationId, activityId, reactionType, _serviceUrl, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity the reaction is on.</param>
    /// <param name="reactionType">The reaction type to remove (for example: "like", "heart", "laugh", etc.).</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task DeleteAsync(string conversationId, string activityId, string reactionType, CancellationToken cancellationToken = default)
    {
        return _client.DeleteReactionAsync(conversationId, activityId, reactionType, _serviceUrl, cancellationToken: cancellationToken);
    }
}
