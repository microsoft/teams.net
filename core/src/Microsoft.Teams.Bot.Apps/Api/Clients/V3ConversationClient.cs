// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Http;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for managing conversations, exposing sub-clients for activities, members, and reactions.
/// </summary>
public class V3ConversationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly BotHttpClient _http;
    private readonly string _serviceUrl;

    /// <summary>
    /// The service URL for this conversation client.
    /// </summary>
    internal string ServiceUrlString => _serviceUrl;

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

    internal V3ConversationClient(string serviceUrl, BotHttpClient http)
    {
        _serviceUrl = serviceUrl.TrimEnd('/');
        _http = http;
        Activities = new ActivityClient(serviceUrl, http);
        Members = new MemberClient(serviceUrl, http);
        Reactions = new ReactionClient(serviceUrl, http);
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    public async Task<CreateConversationResponse?> CreateAsync(ConversationParameters request, CancellationToken cancellationToken = default)
    {
        string url = $"{_serviceUrl}/v3/conversations";
        string body = JsonSerializer.Serialize(request, JsonOptions);
        return await _http.SendAsync<CreateConversationResponse>(HttpMethod.Post, url, body, null, cancellationToken).ConfigureAwait(false);
    }
}
