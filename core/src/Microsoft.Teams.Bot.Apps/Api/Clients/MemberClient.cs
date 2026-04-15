// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for member operations.
/// Delegates to <see cref="CoreConversationClient"/>.
/// </summary>
public class MemberClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultIdentity;

    internal MemberClient(CoreConversationClient client, Uri serviceUrl, AgenticIdentity? defaultIdentity = null)
    {
        _client = client;
        _serviceUrl = serviceUrl;
        _defaultIdentity = defaultIdentity;
    }

    public Task<IList<ConversationAccount>> GetAsync(string conversationId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.GetConversationMembersAsync(conversationId, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public Task<ConversationAccount> GetByIdAsync(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.GetConversationMemberAsync<ConversationAccount>(conversationId, memberId, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteConversationMemberAsync(conversationId, memberId, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }
}
