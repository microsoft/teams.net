// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides reaction operations for adding and removing reactions on activities in conversations.
/// </summary>
public class ReactionsApi
{
    private readonly ConversationClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactionsApi"/> class.
    /// </summary>
    /// <param name="conversationClient">The conversation client for reaction operations.</param>
    internal ReactionsApi(ConversationClient conversationClient)
    {
        _client = conversationClient;
    }

    /// <summary>
    /// Adds a reaction to an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="activityId">The ID of the activity to react to.</param>
    /// <param name="reactionType">The type of reaction to add (e.g., "like", "heart", "laugh").</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddAsync(
        string conversationId,
        string activityId,
        string reactionType,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.AddReactionAsync(conversationId, activityId, reactionType, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Adds a reaction to an activity using activity context.
    /// </summary>
    /// <param name="activity">The activity to react to. Must contain valid Id, Conversation.Id, and ServiceUrl.</param>
    /// <param name="reactionType">The type of reaction to add (e.g., "like", "heart", "laugh").</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddAsync(
        TeamsActivity activity,
        string reactionType,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Id);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        return _client.AddReactionAsync(
            activity.Conversation.Id,
            activity.Id,
            reactionType,
            activity.ServiceUrl,
            activity.Recipient.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="activityId">The ID of the activity to remove the reaction from.</param>
    /// <param name="reactionType">The type of reaction to remove (e.g., "like", "heart", "laugh").</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(
        string conversationId,
        string activityId,
        string reactionType,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.DeleteReactionAsync(conversationId, activityId, reactionType, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Removes a reaction from an activity using activity context.
    /// </summary>
    /// <param name="activity">The activity to remove the reaction from. Must contain valid Id, Conversation.Id, and ServiceUrl.</param>
    /// <param name="reactionType">The type of reaction to remove (e.g., "like", "heart", "laugh").</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(
        TeamsActivity activity,
        string reactionType,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Id);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.Conversation.Id);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);

        return _client.DeleteReactionAsync(
            activity.Conversation.Id,
            activity.Id,
            reactionType,
            activity.ServiceUrl,
            activity.Recipient.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }
}
