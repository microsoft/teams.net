// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for activity operations.
/// Delegates to <see cref="CoreConversationClient"/>.
/// </summary>
public class ActivityClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultIdentity;

    internal ActivityClient(CoreConversationClient client, Uri serviceUrl, AgenticIdentity? defaultIdentity = null)
    {
        _client = client;
        _serviceUrl = serviceUrl;
        _defaultIdentity = defaultIdentity;
    }

    public async Task<SendActivityResponse?> CreateAsync(string conversationId, CoreActivity activity, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        return await _client.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<UpdateActivityResponse> UpdateAsync(string conversationId, string id, CoreActivity activity, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        return await _client.UpdateActivityAsync(conversationId, id, activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, CoreActivity activity, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ReplyToId = id;
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        return await _client.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(string conversationId, string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteActivityAsync(conversationId, id, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public async Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, CoreActivity activity, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
        activity.Recipient ??= new ConversationAccount();
        activity.Recipient.IsTargeted = true;
        return await _client.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task<UpdateActivityResponse> UpdateTargetedAsync(string conversationId, string id, CoreActivity activity, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        return _client.UpdateTargetedActivityAsync(conversationId, id, activity, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }

    public Task DeleteTargetedAsync(string conversationId, string id, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteTargetedActivityAsync(conversationId, id, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }
}
