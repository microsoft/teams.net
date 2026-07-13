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
    private readonly AgenticIdentity? _agenticIdentity;

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

    internal ConversationApiClient(Uri serviceUrl, CoreConversationClient client, AgenticIdentity? agenticIdentity = null)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        _agenticIdentity = agenticIdentity;
        Activities = new ActivityClient(serviceUrl, client, agenticIdentity);
        Members = new MemberClient(serviceUrl, client, agenticIdentity);
        Reactions = new ReactionClient(serviceUrl, client, agenticIdentity);
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    public Task<CreateConversationResponse> CreateAsync(ConversationParameters request, Dictionary<string, string>? additionalHeaders = null, CancellationToken cancellationToken = default)
    {
        return _client.CreateConversationAsync(request, _serviceUrl, requestContext: AgenticContext, customHeaders: additionalHeaders, cancellationToken: cancellationToken);
    }

    private BotRequestContext? AgenticContext => BotRequestContext.FromAgenticIdentity(_agenticIdentity);
}
