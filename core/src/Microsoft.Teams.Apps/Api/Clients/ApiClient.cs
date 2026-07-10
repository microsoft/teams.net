// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

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
/// <item><b>DI-friendly (no serviceUrl)</b> — Use <c>ApiClient(HttpClient, ConversationClient, UserTokenClient, ILogger)</c>
/// and call <c>ForServiceUrl</c> per-request to create a scoped instance.</item>
/// <item><b>Fully initialized</b> — Use <c>ApiClient(Uri, HttpClient, ConversationClient, UserTokenClient, ILogger)</c>
/// when the service URL is known upfront.</item>
/// </list>
/// </remarks>
public class ApiClient
{
    private readonly BotHttpClient _http;

    internal CoreConversationClient ConversationClient { get; }

    internal CoreUserTokenClient UserTokenClient { get; }

    internal BotRequestContext? DefaultRequestContext { get; }

    /// <summary>
    /// The service URL used by this client.
    /// Null when constructed without a service URL (DI-friendly constructor).
    /// Call <c>ForServiceUrl</c> to create a scoped instance with a service URL.
    /// </summary>
    public virtual Uri ServiceUrl { get; }

    /// <summary>
    /// Client for conversation operations (activities, members, reactions).
    /// </summary>
    public virtual ConversationApiClient Conversations { get; }

    private UserTokenApiClient? _userToken;

    /// <summary>
    /// Client for user token operations (OAuth SSO, sign-in resources).
    /// Lazily created over the underlying <see cref="UserTokenClient"/>; serviceUrl-independent.
    /// </summary>
    public virtual UserTokenApiClient UserToken => _userToken ??= new UserTokenApiClient(UserTokenClient);

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
    /// Use <c>ForServiceUrl</c> to create a scoped instance bound to a specific service URL.
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
        DefaultRequestContext = null;

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
    /// <param name="defaultAgenticIdentity">Optional default agentic identity for service URL-bound sub-clients.</param>
    internal ApiClient(Uri serviceUrl, HttpClient httpClient, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, ILogger? logger = null, AgenticIdentity? defaultAgenticIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);
        ArgumentNullException.ThrowIfNull(userTokenClient);

        _http = new BotHttpClient(httpClient, logger);
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        DefaultRequestContext = BotRequestContext.FromAgenticIdentity(defaultAgenticIdentity);
        ServiceUrl = serviceUrl;
        Conversations = new ConversationApiClient(serviceUrl, conversationClient, DefaultRequestContext);
        Teams = new TeamClient(serviceUrl.ToString(), _http, DefaultRequestContext);
        Meetings = new MeetingClient(serviceUrl.ToString(), _http, DefaultRequestContext);
    }

    // Private constructor for ForServiceUrl — shares BotHttpClient, ConversationClient, and UserTokenClient
    private ApiClient(BotHttpClient http, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, Uri serviceUrl, BotRequestContext? defaultRequestContext)
    {
        _http = http;
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        DefaultRequestContext = defaultRequestContext;
        ServiceUrl = serviceUrl;
        Conversations = new ConversationApiClient(serviceUrl, conversationClient, defaultRequestContext);
        Teams = new TeamClient(serviceUrl.ToString(), http, defaultRequestContext);
        Meetings = new MeetingClient(serviceUrl.ToString(), http, defaultRequestContext);
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
        return new ApiClient(_http, ConversationClient, UserTokenClient, serviceUrl, defaultRequestContext: null);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified service URL with a default agentic identity
    /// used when per-call methods do not receive an explicit identity.
    /// </summary>
    /// <param name="serviceUrl">The Bot Framework service URL for this scope.</param>
    /// <param name="defaultAgenticIdentity">The default agentic identity for service URL-bound sub-clients.</param>
    /// <returns>A new <see cref="ApiClient"/> bound to the given service URL and default identity.</returns>
    public virtual ApiClient ForServiceUrl(Uri serviceUrl, AgenticIdentity? defaultAgenticIdentity)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        return new ApiClient(_http, ConversationClient, UserTokenClient, serviceUrl, BotRequestContext.FromAgenticIdentity(defaultAgenticIdentity));
    }
}
