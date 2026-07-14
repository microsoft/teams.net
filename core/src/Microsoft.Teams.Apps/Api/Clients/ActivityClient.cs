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
public class ActivityClient
{
    private const string ObsoleteInboundMessage =
        "Sending an inbound TeamsActivity (read-model) is obsolete. Use the overload that accepts a TeamsActivityInput built via MessageActivityInput.CreateBuilder()/StreamingActivityInput.CreateBuilder().";

    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _agenticIdentity;

    internal ActivityClient(Uri serviceUrl, CoreConversationClient client, AgenticIdentity? agenticIdentity = null)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        _agenticIdentity = agenticIdentity;
    }

    private BotRequestContext? AgenticContext => BotRequestContext.FromAgenticIdentity(_agenticIdentity);

    private Task<SendActivityResponse?> SendCoreAsync(string conversationId, CoreActivityInput activity, bool isTargeted, Dictionary<string, string>? additionalHeaders, CancellationToken cancellationToken)
        => _client.SendActivityAsync(conversationId, activity, _serviceUrl, isTargeted: isTargeted, requestContext: AgenticContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);

    private Task<UpdateActivityResponse> UpdateCoreAsync(string conversationId, string id, CoreActivityInput activity, Dictionary<string, string>? additionalHeaders, CancellationToken cancellationToken)
        => _client.UpdateActivityAsync(conversationId, id, activity, _serviceUrl, requestContext: AgenticContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);

    private Task<UpdateActivityResponse> UpdateTargetedCoreAsync(string conversationId, string id, CoreActivityInput activity, Dictionary<string, string>? additionalHeaders, CancellationToken cancellationToken)
        => _client.UpdateTargetedActivityAsync(conversationId, id, activity, _serviceUrl, requestContext: AgenticContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> CreateAsync(string conversationId, TeamsActivityInput activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return SendCoreAsync(conversationId, activity, isTargeted: false, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Create a new activity in a conversation.
    /// </summary>
    [Obsolete(ObsoleteInboundMessage)]
    public Task<SendActivityResponse?> CreateAsync(string conversationId, TeamsActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return SendCoreAsync(conversationId, CoreActivityInput.FromActivity(activity), isTargeted: false, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    public Task<UpdateActivityResponse> UpdateAsync(string conversationId, string id, TeamsActivityInput activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return UpdateCoreAsync(conversationId, id, activity, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Update an existing activity in a conversation.
    /// </summary>
    [Obsolete(ObsoleteInboundMessage)]
    public Task<UpdateActivityResponse> UpdateAsync(string conversationId, string id, TeamsActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return UpdateCoreAsync(conversationId, id, CoreActivityInput.FromActivity(activity), additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    public Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, TeamsActivityInput activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.Properties["replyToId"] = id;
        return SendCoreAsync(conversationId, activity, isTargeted: false, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Reply to an existing activity in a conversation.
    /// </summary>
    [Obsolete(ObsoleteInboundMessage)]
    public Task<SendActivityResponse?> ReplyAsync(string conversationId, string id, TeamsActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        CoreActivityInput input = CoreActivityInput.FromActivity(activity);
        input.Properties["replyToId"] = id;
        return SendCoreAsync(conversationId, input, isTargeted: false, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Delete an activity from a conversation.
    /// </summary>
    public Task DeleteAsync(string conversationId, string id, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteActivityAsync(conversationId, id, _serviceUrl, requestContext: AgenticContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, TeamsActivityInput activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return SendCoreAsync(conversationId, activity, isTargeted: true, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Create a new targeted activity in a conversation.
    /// Targeted activities are only visible to the specified recipient.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    [Obsolete(ObsoleteInboundMessage)]
    public Task<SendActivityResponse?> CreateTargetedAsync(string conversationId, TeamsActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return SendCoreAsync(conversationId, CoreActivityInput.FromActivity(activity), isTargeted: true, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public Task<UpdateActivityResponse> UpdateTargetedAsync(string conversationId, string id, TeamsActivityInput activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return UpdateTargetedCoreAsync(conversationId, id, activity, additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Update an existing targeted activity in a conversation.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    [Obsolete(ObsoleteInboundMessage)]
    public Task<UpdateActivityResponse> UpdateTargetedAsync(string conversationId, string id, TeamsActivity activity, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return UpdateTargetedCoreAsync(conversationId, id, CoreActivityInput.FromActivity(activity), additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Delete a targeted activity from a conversation.
    /// </summary>
    public Task DeleteTargetedAsync(string conversationId, string id, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.DeleteTargetedActivityAsync(conversationId, id, _serviceUrl, requestContext: AgenticContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }
}
