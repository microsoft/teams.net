// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for managing conversation members.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
public class MemberClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;

    internal MemberClient(Uri serviceUrl, CoreConversationClient client)
    {
        _serviceUrl = serviceUrl;
        _client = client;
    }

    /// <summary>
    /// Get all members of a conversation.
    /// </summary>
    public Task<IList<ConversationAccount>> GetAsync(string conversationId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.GetConversationMembersAsync(conversationId, _serviceUrl, agenticIdentity: agenticIdentity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public Task<T> GetByIdAsync<T>(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default) where T : ConversationAccount
    {
        return _client.GetConversationMemberAsync<T>(conversationId, memberId, _serviceUrl, agenticIdentity: agenticIdentity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public Task<ConversationAccount> GetByIdAsync(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync<ConversationAccount>(conversationId, memberId, agenticIdentity, cancellationToken);
    }
}
