// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for managing conversations, exposing sub-clients for activities, members, and reactions.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
public class ConversationApiClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly BotRequestContext? _requestContext;

    /// <summary>
    /// Client for activity operations.
    /// </summary>
    public ActivityClient Activities { get; }

    /// <summary>
    /// Client for member operations.
    /// </summary>
    public MemberClient Members { get; }

    /// <summary>
    /// Client for reaction operations.
    /// </summary>
    public ReactionClient Reactions { get; }

    internal ConversationApiClient(Uri serviceUrl, CoreConversationClient client, BotRequestContext? requestContext = null)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        _requestContext = requestContext;
        Activities = new ActivityClient(serviceUrl, client, requestContext);
        Members = new MemberClient(serviceUrl, client, requestContext);
        Reactions = new ReactionClient(serviceUrl, client, requestContext);
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    public Task<CreateConversationResponse> CreateAsync(ConversationParameters request, AgenticIdentity? agenticIdentity = null, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return CreateAsync(request, BotRequestContext.FromAgenticIdentity(agenticIdentity), additionalHeaders, cancellationToken);
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    public Task<CreateConversationResponse> CreateAsync(ConversationParameters request, BotRequestContext? requestContext, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.CreateConversationAsync(request, _serviceUrl, requestContext: BotRequestContext.Merge(_requestContext, requestContext), customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }
}
