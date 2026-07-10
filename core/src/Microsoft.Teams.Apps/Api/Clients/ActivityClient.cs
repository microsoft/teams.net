// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for creating, updating, and deleting activities in a conversation.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
[Obsolete("Use the activity methods on ConversationApiClient directly instead.")]
public class ActivityClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultAgenticIdentity;

    internal ActivityClient(Uri serviceUrl, CoreConversationClient client, AgenticIdentity? defaultAgenticIdentity = null)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        _defaultAgenticIdentity = defaultAgenticIdentity;
    }

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> CreateAsync(string conversationId, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => CreateAsync(conversationId, activity, new APIRequestOptions { AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> CreateAsync(string conversationId, CoreActivity activity, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        return _client.SendActivityAsync(activity, requestContext: BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity), customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    public Task<UpdateActivityResponse> UpdateAsync(string conversationId, string id, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => UpdateAsync(conversationId, id, activity, new APIRequestOptions { AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    public Task<UpdateActivityResponse> UpdateAsync(string conversationId, string id, CoreActivity activity, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        BotRequestContext? requestContext = BotRequestContext.Merge(
            BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity),
            BotRequestContext.FromActivity(activity));
        return _client.UpdateActivityAsync(conversationId, id, activity, requestContext: requestContext, customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => ReplyAsync(conversationId, id, activity, new APIRequestOptions { AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, CoreActivity activity, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ReplyToId = id;
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        return _client.SendActivityAsync(activity, requestContext: BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity), customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Delete an activity from a conversation.
    /// </summary>
    public Task DeleteAsync(string conversationId, string id, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => DeleteAsync(conversationId, id, new APIRequestOptions { AgenticIdentity = agenticIdentity, AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Delete an activity from a conversation.
    /// </summary>
    public Task DeleteAsync(string conversationId, string id, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        return _client.DeleteActivityAsync(conversationId, id, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity), customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get members of a specific activity.
    /// </summary>
    public Task<IList<TeamsChannelAccount?>> GetMembersAsync(string conversationId, string id, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => GetMembersAsync(conversationId, id, new APIRequestOptions { AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Get members of a specific activity.
    /// </summary>
    public async Task<IList<TeamsChannelAccount?>> GetMembersAsync(string conversationId, string id, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        IList<ChannelAccount> members = await _client.GetActivityMembersAsync(conversationId, id, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity), customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken).ConfigureAwait(false);
        return [.. members.Select(m => TeamsChannelAccount.FromChannelAccount(m))];
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => CreateTargetedAsync(conversationId, activity, new APIRequestOptions { AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, CoreActivity activity, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        // Ensure recipient is marked as targeted
        if (activity.Recipient is not null)
        {
            activity.Recipient.IsTargeted = true;
        }
        return _client.SendActivityAsync(activity, requestContext: BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity), customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<UpdateActivityResponse> UpdateTargetedAsync(string conversationId, string id, CoreActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => UpdateTargetedAsync(conversationId, id, activity, new APIRequestOptions { AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<UpdateActivityResponse> UpdateTargetedAsync(string conversationId, string id, CoreActivity activity, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        BotRequestContext? requestContext = BotRequestContext.Merge(
            BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity),
            BotRequestContext.FromActivity(activity));
        return _client.UpdateTargetedActivityAsync(conversationId, id, activity, requestContext: requestContext, customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    public Task DeleteTargetedAsync(string conversationId, string id, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
        => DeleteTargetedAsync(conversationId, id, new APIRequestOptions { AgenticIdentity = agenticIdentity, AdditionalHeaders = additionalHeaders }, cancellationToken);

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    public Task DeleteTargetedAsync(string conversationId, string id, APIRequestOptions options, CancellationToken cancellationToken = default)
    {
        return _client.DeleteTargetedActivityAsync(conversationId, id, _serviceUrl, requestContext: BotRequestContext.FromAgenticIdentity(options.AgenticIdentity ?? _defaultAgenticIdentity), customHeaders: options.AdditionalHeaders, cancellationToken: cancellationToken);
    }
}
