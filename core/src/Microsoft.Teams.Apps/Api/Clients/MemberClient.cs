// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;

namespace Microsoft.Teams.Apps.Api.Clients;

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
    [Obsolete("Use GetPagedAsync instead.")]
    public async Task<IList<TeamsChannelAccount?>> GetAsync(string conversationId, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        IList<ChannelAccount> members = await _client.GetConversationMembersAsync(conversationId, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken).ConfigureAwait(false);
        return [.. members.Select(m => TeamsChannelAccount.FromChannelAccount(m))];
    }

    /// <summary>
    /// Get members of a conversation with pagination support.
    /// </summary>
    public async Task<PagedTeamsMembersResult> GetPagedAsync(
        string conversationId,
        int pageSize = 50,
        string? continuationToken = null,
        AgenticIdentity? agenticIdentity = null,
        Dictionary<string, string>? additionalHeaders = null,
        CancellationToken cancellationToken = default)
    {
        PagedMembersResult? paged = await _client.GetConversationPagedMembersAsync(
            conversationId,
            _serviceUrl,
            pageSize,
            continuationToken,
            requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity),
            customHeaders: additionalHeaders,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        PagedTeamsMembersResult result = new();
        if (paged is not null)
        {
            result.ContinuationToken = paged.ContinuationToken;
            if (paged.Members is not null)
            {
                result.Members = [.. paged.Members.Select(m => TeamsChannelAccount.FromChannelAccount(m))];
            }
        }
        return result;
    }

    /// <summary>
    /// Get all members of a conversation, automatically following pagination.
    /// </summary>
    /// <remarks>
    /// Streams members across all pages by following the continuation token internally,
    /// for convenient <c>await foreach</c> iteration. Use <see cref="GetPagedAsync"/> when you
    /// need explicit control over paging (for example, to persist the continuation token across
    /// requests and resume later).
    /// </remarks>
    public async IAsyncEnumerable<TeamsChannelAccount> GetAllAsync(
        string conversationId,
        int pageSize = 50,
        AgenticIdentity? agenticIdentity = null,
        Dictionary<string, string>? additionalHeaders = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? continuationToken = null;
        do
        {
            PagedTeamsMembersResult page = await GetPagedAsync(
                conversationId,
                pageSize,
                continuationToken,
                agenticIdentity,
                additionalHeaders,
                cancellationToken).ConfigureAwait(false);

            foreach (TeamsChannelAccount? member in page.Members)
            {
                if (member is not null)
                {
                    yield return member;
                }
            }

            continuationToken = page.ContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public Task<T> GetByIdAsync<T>(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default) where T : ChannelAccount
    {
        return _client.GetConversationMemberAsync<T>(conversationId, memberId, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public async Task<TeamsChannelAccount?> GetByIdAsync(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ChannelAccount member = await GetByIdAsync<ChannelAccount>(conversationId, memberId, agenticIdentity, additionalHeaders, cancellationToken).ConfigureAwait(false);
        return TeamsChannelAccount.FromChannelAccount(member);
    }
}
