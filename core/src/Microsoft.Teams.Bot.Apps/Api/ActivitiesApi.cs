// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides activity operations for sending, updating, and deleting activities in conversations.
/// </summary>
public class ActivitiesApi
{
    private readonly ConversationClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivitiesApi"/> class.
    /// </summary>
    /// <param name="conversationClient">The conversation client for activity operations.</param>
    internal ActivitiesApi(ConversationClient conversationClient)
    {
        _client = conversationClient;
    }

    /// <summary>
    /// Sends an activity to a conversation.
    /// </summary>
    /// <param name="activity">The activity to send. Must contain valid conversation and service URL information.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the sent activity.</returns>
    public Task<SendActivityResponse> SendAsync(
        CoreActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendActivityAsync(activity, customHeaders, cancellationToken);

    /// <summary>
    /// Updates an existing activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="activityId">The ID of the activity to update.</param>
    /// <param name="activity">The updated activity data.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with the ID of the updated activity.</returns>
    public Task<UpdateActivityResponse> UpdateAsync(
        string conversationId,
        string activityId,
        CoreActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.UpdateActivityAsync(conversationId, activityId, activity, customHeaders, cancellationToken);

    /// <summary>
    /// Deletes an existing activity from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="activityId">The ID of the activity to delete.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(
        string conversationId,
        string activityId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.DeleteActivityAsync(conversationId, activityId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Deletes an existing activity from a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity to delete. Must contain valid Id, Conversation.Id, and ServiceUrl.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(
        CoreActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.DeleteActivityAsync(activity, customHeaders, cancellationToken);

    /// <summary>
    /// Deletes an existing activity from a conversation using Teams activity context.
    /// </summary>
    /// <param name="activity">The Teams activity to delete. Must contain valid Id, Conversation.Id, and ServiceUrl.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.DeleteActivityAsync(activity, customHeaders, cancellationToken);

    /// <summary>
    /// Uploads and sends historic activities to a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="transcript">The transcript containing the historic activities.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with a resource ID.</returns>
    public Task<SendConversationHistoryResponse> SendHistoryAsync(
        string conversationId,
        Transcript transcript,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.SendConversationHistoryAsync(conversationId, transcript, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Uploads and sends historic activities to a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity providing conversation context. Must contain valid Conversation.Id and ServiceUrl.</param>
    /// <param name="transcript">The transcript containing the historic activities.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response with a resource ID.</returns>
    public Task<SendConversationHistoryResponse> SendHistoryAsync(
        TeamsActivity activity,
        Transcript transcript,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.SendConversationHistoryAsync(
            activity.Conversation.Id!,
            transcript,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets the members of a specific activity.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="activityId">The ID of the activity.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of members for the activity.</returns>
    public Task<IList<ConversationAccount>> GetMembersAsync(
        string conversationId,
        string activityId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.GetActivityMembersAsync(conversationId, activityId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets the members of a specific activity using activity context.
    /// </summary>
    /// <param name="activity">The activity to get members for. Must contain valid Id, Conversation.Id, and ServiceUrl.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of members for the activity.</returns>
    public Task<IList<ConversationAccount>> GetMembersAsync(
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetActivityMembersAsync(
            activity.Conversation.Id!,
            activity.Id!,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }
}
