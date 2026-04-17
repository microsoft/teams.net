// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for managing conversations, exposing sub-clients for activities, members, and reactions.
/// Delegates to the core <see cref="CoreConversationClient"/>.
/// </summary>
public class V3ConversationClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;

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

    internal V3ConversationClient(Uri serviceUrl, CoreConversationClient client)
    {
        _serviceUrl = serviceUrl;
        _client = client;
        Activities = new ActivityClient(serviceUrl, client);
        Members = new MemberClient(serviceUrl, client);
        Reactions = new ReactionClient(serviceUrl, client);
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    public Task<CreateConversationResponse> CreateAsync(ConversationParameters request, CancellationToken cancellationToken = default)
    {
        return _client.CreateConversationAsync(request, _serviceUrl, cancellationToken: cancellationToken);
    }
}
