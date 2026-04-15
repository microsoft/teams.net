// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;
using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Factory for creating <see cref="ApiClient"/> instances.
/// Register as a singleton in DI and use <see cref="Create"/> to build request-scoped clients.
/// </summary>
/// <remarks>
/// The factory holds references to the DI-injected SDK clients (which are singletons).
/// Each <see cref="ApiClient"/> instance is scoped to a specific <c>serviceUrl</c> and optional
/// <see cref="AgenticIdentity"/>.
/// </remarks>
public class ApiClientFactory
{
    private readonly CoreConversationClient _conversationClient;
    private readonly CoreUserTokenClient _userTokenClient;
    private readonly TeamsApiClient _teamsApiClient;

    public ApiClientFactory(CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, TeamsApiClient teamsApiClient)
    {
        _conversationClient = conversationClient;
        _userTokenClient = userTokenClient;
        _teamsApiClient = teamsApiClient;
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified service URL.
    /// </summary>
    /// <param name="serviceUrl">The Teams service URL for this client instance.</param>
    /// <param name="agenticIdentity">Optional default agentic identity for all calls made through this client.</param>
    /// <returns>A new <see cref="ApiClient"/> instance.</returns>
    public ApiClient Create(Uri serviceUrl, AgenticIdentity? agenticIdentity = null)
    {
        return new ApiClient(_conversationClient, _userTokenClient, _teamsApiClient, serviceUrl, agenticIdentity);
    }
}
