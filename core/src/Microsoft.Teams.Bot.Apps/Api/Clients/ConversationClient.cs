// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for conversation operations.
/// Aggregates <see cref="ActivityClient"/>, <see cref="MemberClient"/>, and <see cref="ReactionClient"/>.
/// Delegates to <see cref="CoreConversationClient"/>.
/// </summary>
public class ConversationClient
{
    private readonly CoreConversationClient _client;
    private readonly Uri _serviceUrl;
    private readonly AgenticIdentity? _defaultIdentity;

    internal ConversationClient(CoreConversationClient client, Uri serviceUrl, AgenticIdentity? defaultIdentity = null)
    {
        _client = client;
        _serviceUrl = serviceUrl;
        _defaultIdentity = defaultIdentity;
        Activities = new ActivityClient(client, serviceUrl, defaultIdentity);
        Members = new MemberClient(client, serviceUrl, defaultIdentity);
        Reactions = new ReactionClient(client, serviceUrl, defaultIdentity);
    }

    public ActivityClient Activities { get; }

    public MemberClient Members { get; }

    public ReactionClient Reactions { get; }

    public Task<CreateConversationResponse> CreateAsync(ConversationParameters request, AgenticIdentity? agenticIdentity = null, CancellationToken cancellationToken = default)
    {
        return _client.CreateConversationAsync(request, _serviceUrl, agenticIdentity ?? _defaultIdentity, cancellationToken: cancellationToken);
    }
}
