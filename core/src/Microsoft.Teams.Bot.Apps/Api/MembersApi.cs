// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides member operations for managing conversation members.
/// </summary>
public class MembersApi
{
    private readonly ConversationClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MembersApi"/> class.
    /// </summary>
    /// <param name="conversationClient">The conversation client for member operations.</param>
    internal MembersApi(ConversationClient conversationClient)
    {
        _client = conversationClient;
    }

    /// <summary>
    /// Gets all members of a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of conversation members.</returns>
    public Task<IList<ConversationAccount>> GetAllAsync(
        string conversationId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.GetConversationMembersAsync(conversationId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets all members of a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity providing conversation context. Must contain valid Conversation.Id and ServiceUrl.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of conversation members.</returns>
    public Task<IList<ConversationAccount>> GetAllAsync(
        TeamsActivity activity,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetConversationMembersAsync(
            activity.Conversation.Id!,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets a specific member of a conversation.
    /// </summary>
    /// <typeparam name="T">The type of conversation account to return. Must inherit from <see cref="ConversationAccount"/>.</typeparam>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation member.</returns>
    public Task<T> GetByIdAsync<T>(
        string conversationId,
        string userId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default) where T : ConversationAccount
        => _client.GetConversationMemberAsync<T>(conversationId, userId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets a specific member of a conversation using activity context.
    /// </summary>
    /// <typeparam name="T">The type of conversation account to return. Must inherit from <see cref="ConversationAccount"/>.</typeparam>
    /// <param name="activity">The activity providing conversation context. Must contain valid Conversation.Id and ServiceUrl.</param>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation member.</returns>
    public Task<T> GetByIdAsync<T>(
        TeamsActivity activity,
        string userId,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default) where T : ConversationAccount
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetConversationMemberAsync<T>(
            activity.Conversation.Id!,
            userId,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets a specific member of a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation member.</returns>
    public Task<ConversationAccount> GetByIdAsync(
        string conversationId,
        string userId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.GetConversationMemberAsync<ConversationAccount>(conversationId, userId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets a specific member of a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity providing conversation context. Must contain valid Conversation.Id and ServiceUrl.</param>
    /// <param name="userId">The ID of the user to retrieve.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation member.</returns>
    public Task<ConversationAccount> GetByIdAsync(
        TeamsActivity activity,
        string userId,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetConversationMemberAsync<ConversationAccount>(
            activity.Conversation.Id!,
            userId,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Gets members of a conversation one page at a time.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="pageSize">Optional page size for the number of members to retrieve.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a page of members and an optional continuation token.</returns>
    public Task<PagedMembersResult> GetPagedAsync(
        string conversationId,
        Uri serviceUrl,
        int? pageSize = null,
        string? continuationToken = null,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.GetConversationPagedMembersAsync(conversationId, serviceUrl, pageSize, continuationToken, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Gets members of a conversation one page at a time using activity context.
    /// </summary>
    /// <param name="activity">The activity providing conversation context. Must contain valid Conversation.Id and ServiceUrl.</param>
    /// <param name="pageSize">Optional page size for the number of members to retrieve.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a page of members and an optional continuation token.</returns>
    public Task<PagedMembersResult> GetPagedAsync(
        TeamsActivity activity,
        int? pageSize = null,
        string? continuationToken = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetConversationPagedMembersAsync(
            activity.Conversation.Id!,
            activity.ServiceUrl!,
            pageSize,
            continuationToken,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }

    /// <summary>
    /// Deletes a member from a conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="memberId">The ID of the member to delete.</param>
    /// <param name="serviceUrl">The service URL for the conversation.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>If the deleted member was the last member of the conversation, the conversation is also deleted.</remarks>
    public Task DeleteAsync(
        string conversationId,
        string memberId,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.DeleteConversationMemberAsync(conversationId, memberId, serviceUrl, agenticIdentity, customHeaders, cancellationToken);

    /// <summary>
    /// Deletes a member from a conversation using activity context.
    /// </summary>
    /// <param name="activity">The activity providing conversation context. Must contain valid Conversation.Id and ServiceUrl.</param>
    /// <param name="memberId">The ID of the member to delete.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>If the deleted member was the last member of the conversation, the conversation is also deleted.</remarks>
    public Task DeleteAsync(
        TeamsActivity activity,
        string memberId,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.DeleteConversationMemberAsync(
            activity.Conversation.Id!,
            memberId,
            activity.ServiceUrl!,
            activity.From.GetAgenticIdentity(),
            customHeaders,
            cancellationToken);
    }
}
