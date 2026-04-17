// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core.Http;

using CoreConversationClient = Microsoft.Teams.Bot.Core.ConversationClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Top-level API client that provides access to all Teams Bot API sub-clients.
/// </summary>
/// <remarks>
/// <para>
/// This client can be constructed in two ways:
/// </para>
/// <list type="bullet">
/// <item><b>DI-friendly (no serviceUrl)</b> — Use <see cref="ApiClient(HttpClient, CoreConversationClient, ILogger, string)"/>
/// and call <see cref="ForServiceUrl"/> per-request to create a scoped instance.</item>
/// <item><b>Fully initialized</b> — Use <see cref="ApiClient(Uri, HttpClient, CoreConversationClient, ILogger, string)"/>
/// when the service URL is known upfront.</item>
/// </list>
/// </remarks>
public class ApiClient
{
    private readonly BotHttpClient _http;
    private readonly CoreConversationClient _conversationClient;
    private readonly string _tokenApiEndpoint;

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
    public virtual V3ConversationClient Conversations { get; }

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
    /// <param name="logger">Optional logger.</param>
    /// <param name="tokenApiEndpoint">Optional token API endpoint override. Defaults to https://token.botframework.com.</param>
    [ActivatorUtilitiesConstructor]
    public ApiClient(HttpClient httpClient, CoreConversationClient conversationClient, ILogger? logger = null, string tokenApiEndpoint = "https://token.botframework.com")
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);

        _http = new BotHttpClient(httpClient, logger);
        _conversationClient = conversationClient;
        _tokenApiEndpoint = tokenApiEndpoint;
        Bots = new BotClient(_http, tokenApiEndpoint);
        Users = new UserClient(_http, tokenApiEndpoint);

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
    /// <param name="logger">Optional logger.</param>
    /// <param name="tokenApiEndpoint">Optional token API endpoint override. Defaults to https://token.botframework.com.</param>
    public ApiClient(Uri serviceUrl, HttpClient httpClient, CoreConversationClient conversationClient, ILogger? logger = null, string tokenApiEndpoint = "https://token.botframework.com")
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);

        _http = new BotHttpClient(httpClient, logger);
        _conversationClient = conversationClient;
        _tokenApiEndpoint = tokenApiEndpoint;
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, tokenApiEndpoint);
        Conversations = new V3ConversationClient(serviceUrl, conversationClient);
        Users = new UserClient(_http, tokenApiEndpoint);
        Teams = new TeamClient(serviceUrl.ToString(), _http);
        Meetings = new MeetingClient(serviceUrl.ToString(), _http);
    }

    /// <summary>
    /// Creates a copy of an existing <see cref="ApiClient"/> with the same configuration.
    /// </summary>
    public ApiClient(ApiClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        ServiceUrl = client.ServiceUrl;
        _http = client._http;
        _conversationClient = client._conversationClient;
        _tokenApiEndpoint = client._tokenApiEndpoint;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
    }

    // Private constructor for ForServiceUrl — shares BotHttpClient and ConversationClient
    private ApiClient(BotHttpClient http, CoreConversationClient conversationClient, string tokenApiEndpoint, Uri serviceUrl)
    {
        _http = http;
        _conversationClient = conversationClient;
        _tokenApiEndpoint = tokenApiEndpoint;
        ServiceUrl = serviceUrl;
        Bots = new BotClient(http, tokenApiEndpoint);
        Conversations = new V3ConversationClient(serviceUrl, conversationClient);
        Users = new UserClient(http, tokenApiEndpoint);
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
        return new ApiClient(_http, _conversationClient, _tokenApiEndpoint, serviceUrl);
    }
}
