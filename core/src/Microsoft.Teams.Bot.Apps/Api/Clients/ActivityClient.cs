// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for creating, updating, and deleting activities in a conversation.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
public class ActivityClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;

    internal ActivityClient(Uri serviceUrl, CoreConversationClient client)
    {
        _serviceUrl = serviceUrl;
        _client = client;
    }

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> CreateAsync(string conversationId, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        EnsureActivityContext(activity, conversationId);
        return _client.SendActivityAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    public Task<UpdateActivityResponse> UpdateAsync(string conversationId, string id, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        return _client.UpdateActivityAsync(conversationId, id, activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ReplyToId = id;
        EnsureActivityContext(activity, conversationId);
        return _client.SendActivityAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Delete an activity from a conversation.
    /// </summary>
    public Task DeleteAsync(string conversationId, string id, CancellationToken cancellationToken = default)
    {
        return _client.DeleteActivityAsync(conversationId, id, _serviceUrl, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    public Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        EnsureActivityContext(activity, conversationId);
        EnsureTargeted(activity);
        return _client.SendActivityAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    public Task<UpdateActivityResponse> UpdateTargetedAsync(string conversationId, string id, CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl ??= _serviceUrl;
        return _client.UpdateTargetedActivityAsync(conversationId, id, activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    public Task DeleteTargetedAsync(string conversationId, string id, CancellationToken cancellationToken = default)
    {
        return _client.DeleteTargetedActivityAsync(conversationId, id, _serviceUrl, cancellationToken: cancellationToken);
    }

    private void EnsureActivityContext(CoreActivity activity, string conversationId)
    {
        activity.ServiceUrl ??= _serviceUrl;
        activity.Conversation ??= new Conversation(conversationId);
    }

    private static void EnsureTargeted(CoreActivity activity)
    {
        activity.Recipient ??= new ConversationAccount();
        activity.Recipient.IsTargeted = true;
    }
}
