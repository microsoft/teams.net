// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core.Http;

using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;
using CoreUserTokenClient = Microsoft.Teams.Core.UserTokenClient;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Top-level API client that provides access to all Teams Bot API sub-clients.
/// </summary>
/// <remarks>
/// <para>
/// This client can be constructed in two ways:
/// </para>
/// <list type="bullet">
/// <item><b>DI-friendly (no serviceUrl)</b> — Use <see cref="ApiClient(HttpClient, CoreConversationClient, CoreUserTokenClient, ILogger)"/>
/// and call <see cref="ForServiceUrl"/> per-request to create a scoped instance.</item>
/// <item><b>Fully initialized</b> — Use <see cref="ApiClient(Uri, HttpClient, CoreConversationClient, CoreUserTokenClient, ILogger)"/>
/// when the service URL is known upfront.</item>
/// </list>
/// </remarks>
public class ApiClient
{
    private readonly BotHttpClient _http;

    internal CoreConversationClient ConversationClient { get; }

    internal CoreUserTokenClient UserTokenClient { get; }

    /// <summary>
    /// The service URL used by this client.
    /// Null when constructed without a service URL (DI-friendly constructor).
    /// Call <see cref="ForServiceUrl"/> to create a scoped instance with a service URL.
    /// </summary>
    public virtual Uri ServiceUrl { get; }

    /// <summary>
    /// Client for bot-level operations (token, sign-in).
    /// </summary>
    public virtual BotClient Bots { get; }

    /// <summary>
    /// Client for conversation operations (activities, members, reactions).
    /// </summary>
    public virtual ConversationApiClient Conversations { get; }

    /// <summary>
    /// Client for user-level operations (token).
    /// </summary>
    public virtual UserClient Users { get; }

    /// <summary>
    /// Client for team operations.
    /// </summary>
    public virtual TeamClient Teams { get; }

    /// <summary>
    /// Client for meeting operations.
    /// </summary>
    public virtual MeetingClient Meetings { get; }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> without a service URL (DI-friendly).
    /// Use <see cref="ForServiceUrl"/> to create a scoped instance bound to a specific service URL.
    /// </summary>
    /// <param name="httpClient">An <see cref="HttpClient"/> configured with authentication (e.g., via DI with <c>BotAuthenticationHandler</c>).</param>
    /// <param name="conversationClient">The core conversation client for conversation/activity/member operations.</param>
    /// <param name="userTokenClient">The core user token client for sign-in and token operations.</param>
    /// <param name="logger">Optional logger.</param>
    [ActivatorUtilitiesConstructor]
    internal ApiClient(HttpClient httpClient, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);
        ArgumentNullException.ThrowIfNull(userTokenClient);

        _http = new BotHttpClient(httpClient, logger);
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        Bots = new BotClient(userTokenClient);
        Users = new UserClient(userTokenClient);

        // ServiceUrl-dependent sub-clients require ForServiceUrl() before use
        ServiceUrl = null!;
        Conversations = null!;
        Teams = null!;
        Meetings = null!;
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> bound to a specific service URL.
    /// </summary>
    /// <param name="serviceUrl">The Bot Framework service URL.</param>
    /// <param name="httpClient">An <see cref="HttpClient"/> configured with authentication (e.g., via DI with <c>BotAuthenticationHandler</c>).</param>
    /// <param name="conversationClient">The core conversation client for conversation/activity/member operations.</param>
    /// <param name="userTokenClient">The core user token client for sign-in and token operations.</param>
    /// <param name="logger">Optional logger.</param>
    internal ApiClient(Uri serviceUrl, HttpClient httpClient, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);
        ArgumentNullException.ThrowIfNull(userTokenClient);

        _http = new BotHttpClient(httpClient, logger);
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        ServiceUrl = serviceUrl;
        Bots = new BotClient(userTokenClient);
        Conversations = new ConversationApiClient(serviceUrl, conversationClient);
        Users = new UserClient(userTokenClient);
        Teams = new TeamClient(serviceUrl.ToString(), _http);
        Meetings = new MeetingClient(serviceUrl.ToString(), _http);
    }

    /// <summary>
    /// Creates a copy of an existing <see cref="ApiClient"/> with the same configuration.
    /// </summary>
    internal ApiClient(ApiClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        ServiceUrl = client.ServiceUrl;
        _http = client._http;
        ConversationClient = client.ConversationClient;
        UserTokenClient = client.UserTokenClient;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
    }

    // Private constructor for ForServiceUrl — shares BotHttpClient, ConversationClient, and UserTokenClient
    private ApiClient(BotHttpClient http, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, Uri serviceUrl)
    {
        _http = http;
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        ServiceUrl = serviceUrl;
        Bots = new BotClient(userTokenClient);
        Conversations = new ConversationApiClient(serviceUrl, conversationClient);
        Users = new UserClient(userTokenClient);
        Teams = new TeamClient(serviceUrl.ToString(), http);
        Meetings = new MeetingClient(serviceUrl.ToString(), http);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified service URL,
    /// sharing the underlying HTTP client and authentication.
    /// </summary>
    /// <param name="serviceUrl">The Bot Framework service URL for this scope.</param>
    /// <returns>A new <see cref="ApiClient"/> bound to the given service URL.</returns>
    public virtual ApiClient ForServiceUrl(Uri serviceUrl)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        return new ApiClient(_http, ConversationClient, UserTokenClient, serviceUrl);
    }
}
