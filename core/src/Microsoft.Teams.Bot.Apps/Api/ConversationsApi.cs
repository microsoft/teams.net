// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Provides conversation-related operations.
/// </summary>
/// <remarks>
/// This class serves as a container for conversation-specific sub-APIs:
/// <list type="bullet">
/// <item><see cref="Activities"/> - Activity operations (send, update, delete, history)</item>
/// <item><see cref="Members"/> - Member operations (get, delete)</item>
/// <item><see cref="Reactions"/> - Reaction operations (add, delete)</item>
/// </list>
/// </remarks>
public class ConversationsApi
{
    private readonly ConversationClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationsApi"/> class.
    /// </summary>
    /// <param name="conversationClient">The conversation client for conversation operations.</param>
    internal ConversationsApi(ConversationClient conversationClient)
    {
        _client = conversationClient;
        Activities = new ActivitiesApi(conversationClient);
        Members = new MembersApi(conversationClient);
        Reactions = new ReactionsApi(conversationClient);
    }

    /// <summary>
    /// Gets the activities API for sending, updating, and deleting activities.
    /// </summary>
    public ActivitiesApi Activities { get; }

    /// <summary>
    /// Gets the members API for managing conversation members.
    /// </summary>
    public MembersApi Members { get; }

    /// <summary>
    /// Gets the reactions API for adding and removing reactions on activities.
    /// </summary>
    public ReactionsApi Reactions { get; }

    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="parameters">The parameters for creating the conversation. Cannot be null.</param>
    /// <param name="serviceUrl">The service URL for the bot. Cannot be null.</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="customHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the conversation resource response with the conversation ID.</returns>
    public Task<CreateConversationResponse> CreateAsync(
        ConversationParameters parameters,
        Uri serviceUrl,
        AgenticIdentity? agenticIdentity = null,
        CustomHeaders? customHeaders = null,
        CancellationToken cancellationToken = default)
        => _client.CreateConversationAsync(parameters, serviceUrl, agenticIdentity, customHeaders, cancellationToken);
}
