// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for managing conversations, including activities, members, and reactions.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
public class ConversationApiClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultAgenticIdentity;

    /// <summary>
    /// Client for activity operations.
    /// </summary>
    [Obsolete("Use the activity methods on ConversationApiClient directly instead.")]
    public ActivityClient Activities { get; }

    /// <summary>
    /// Client for member operations.
    /// </summary>
    [Obsolete("Use the member methods on ConversationApiClient directly instead.")]
    public MemberClient Members { get; }

    /// <summary>
    /// Client for reaction operations.
    /// </summary>
    [Obsolete("Use ConversationApiClient.AddReactionAsync and ConversationApiClient.DeleteReactionAsync instead.")]
    public ReactionClient Reactions { get; }

    internal ConversationApiClient(Uri serviceUrl, CoreConversationClient client, AgenticIdentity? defaultAgenticIdentity = null)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        _defaultAgenticIdentity = defaultAgenticIdentity;
#pragma warning disable CS0618 // Suppress obsolete warnings for backward-compatible initialization
        Activities = new ActivityClient(serviceUrl, client, defaultAgenticIdentity);
        Members = new MemberClient(serviceUrl, client, defaultAgenticIdentity);
        Reactions = new ReactionClient(serviceUrl, client, defaultAgenticIdentity);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    public Task<CreateConversationResponse> CreateAsync(ConversationParameters request, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.CreateConversationAsync(request, _serviceUrl, requestContext: ApiRequestContext.Resolve(_defaultAgenticIdentity, agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    #region Activity Methods

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> CreateActivityAsync(string conversationId, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        return _client.SendActivityAsync(activity, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    public Task<UpdateActivityResponse> UpdateActivityAsync(string conversationId, string id, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        return _client.UpdateActivityAsync(conversationId, id, activity, requestContext: BotRequestContext.FromActivity(activity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> ReplyToActivityAsync(string conversationId, string id, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ReplyToId = id;
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        return _client.SendActivityAsync(activity, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Delete an activity from a conversation.
    /// </summary>
    public Task DeleteActivityAsync(string conversationId, string id, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteActivityAsync(conversationId, id, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<SendActivityResponse?> CreateTargetedActivityAsync(string conversationId, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        if (activity.Recipient is not null)
        {
            activity.Recipient.IsTargeted = true;
        }
        return _client.SendActivityAsync(activity, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<UpdateActivityResponse> UpdateTargetedActivityAsync(string conversationId, string id, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        return _client.UpdateTargetedActivityAsync(conversationId, id, activity, requestContext: BotRequestContext.FromActivity(activity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    public Task DeleteTargetedActivityAsync(string conversationId, string id, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteTargetedActivityAsync(conversationId, id, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    #endregion

    #region Member Methods

    /// <summary>
    /// Get all members of a conversation.
    /// </summary>
    [Obsolete("Use GetMembersPagedAsync instead.")]
    public async Task<IList<TeamsChannelAccount?>> GetMembersAsync(string conversationId, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        IList<ChannelAccount> members = await _client.GetConversationMembersAsync(conversationId, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken).ConfigureAwait(false);
        return [.. members.Select(m => TeamsChannelAccount.FromChannelAccount(m))];
    }

    /// <summary>
    /// Get members of a conversation with pagination support.
    /// </summary>
    public async Task<PagedTeamsMembersResult> GetMembersPagedAsync(
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
    /// for convenient <c>await foreach</c> iteration. Use <see cref="GetMembersPagedAsync"/> when you
    /// need explicit control over paging (for example, to persist the continuation token across
    /// requests and resume later).
    /// </remarks>
    public async IAsyncEnumerable<TeamsChannelAccount> GetAllMembersAsync(
        string conversationId,
        int pageSize = 50,
        AgenticIdentity? agenticIdentity = null,
        Dictionary<string, string>? additionalHeaders = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? ct = null;
        do
        {
            PagedTeamsMembersResult page = await GetMembersPagedAsync(
                conversationId,
                pageSize,
                ct,
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

            ct = page.ContinuationToken;
        } while (!string.IsNullOrEmpty(ct));
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public Task<T> GetMemberByIdAsync<T>(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default) where T : ChannelAccount
    {
        return _client.GetConversationMemberAsync<T>(conversationId, memberId, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get a specific member of a conversation by ID.
    /// </summary>
    public async Task<TeamsChannelAccount?> GetMemberByIdAsync(string conversationId, string memberId, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ChannelAccount member = await GetMemberByIdAsync<ChannelAccount>(conversationId, memberId, agenticIdentity, additionalHeaders, cancellationToken).ConfigureAwait(false);
        return TeamsChannelAccount.FromChannelAccount(member);
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// Adds a reaction on an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity to react to.</param>
    /// <param name="reactionType">The reaction type (for example: "like", "heart", "laugh", etc.).</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="additionalHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task AddReactionAsync(string conversationId, string activityId, string reactionType, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.AddReactionAsync(conversationId, activityId, reactionType, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Removes a reaction from an activity in a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="activityId">The id of the activity the reaction is on.</param>
    /// <param name="reactionType">The reaction type to remove (for example: "like", "heart", "laugh", etc.).</param>
    /// <param name="agenticIdentity">Optional agentic identity for authentication.</param>
    /// <param name="additionalHeaders">Optional custom headers to include in the request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task DeleteReactionAsync(string conversationId, string activityId, string reactionType, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteReactionAsync(conversationId, activityId, reactionType, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(agenticIdentity), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    #endregion
}
