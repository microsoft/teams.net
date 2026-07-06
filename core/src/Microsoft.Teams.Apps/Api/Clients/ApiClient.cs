// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Schema;
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
/// <item><b>Fully initialized</b> — Use the service URL constructor
/// when the service URL is known upfront.</item>
/// </list>
/// </remarks>
public class ApiClient
{
    private readonly BotHttpClient _http;
    private readonly BotRequestContext? _requestContext;

    internal CoreConversationClient ConversationClient { get; }

    internal CoreUserTokenClient UserTokenClient { get; }

    /// <summary>
    /// The service URL used by this client.
    /// Null when constructed without a service URL (DI-friendly constructor).
    /// Call <see cref="ForServiceUrl"/> to create a scoped instance with a service URL.
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
    public virtual UserTokenApiClient UserToken => _userToken ??= new UserTokenApiClient(UserTokenClient, _requestContext);

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
    /// <param name="requestContext">Optional default request context used when an API method does not receive one.</param>
    internal ApiClient(Uri serviceUrl, HttpClient httpClient, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, ILogger? logger = null, BotRequestContext? requestContext = null)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);
        ArgumentNullException.ThrowIfNull(userTokenClient);

        _http = new BotHttpClient(httpClient, logger);
        _requestContext = requestContext;
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        ServiceUrl = serviceUrl;
        Conversations = new ConversationApiClient(serviceUrl, conversationClient, requestContext);
        Teams = new TeamClient(serviceUrl.ToString(), _http, requestContext);
        Meetings = new MeetingClient(serviceUrl.ToString(), _http, requestContext);
    }

    // Private constructors for scoped clients — share BotHttpClient, ConversationClient, and UserTokenClient.
    private ApiClient(BotHttpClient http, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, BotRequestContext? requestContext)
    {
        _http = http;
        _requestContext = requestContext;
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;

        // ServiceUrl-dependent sub-clients require ForServiceUrl() before use
        ServiceUrl = null!;
        Conversations = null!;
        Teams = null!;
        Meetings = null!;
    }

    private ApiClient(BotHttpClient http, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, Uri serviceUrl, BotRequestContext? requestContext)
    {
        _http = http;
        _requestContext = requestContext;
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        ServiceUrl = serviceUrl;
        Conversations = new ConversationApiClient(serviceUrl, conversationClient, requestContext);
        Teams = new TeamClient(serviceUrl.ToString(), http, requestContext);
        Meetings = new MeetingClient(serviceUrl.ToString(), http, requestContext);
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
        return new ApiClient(_http, ConversationClient, UserTokenClient, serviceUrl, _requestContext);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified request context.
    /// </summary>
    /// <param name="requestContext">The request context used when an API method does not receive one.</param>
    /// <returns>A new <see cref="ApiClient"/> with the request context applied.</returns>
    public virtual ApiClient ForRequestContext(BotRequestContext? requestContext)
    {
        BotRequestContext? mergedRequestContext = BotRequestContext.Merge(_requestContext, requestContext);
        return ServiceUrl is null
            ? new ApiClient(_http, ConversationClient, UserTokenClient, mergedRequestContext)
            : new ApiClient(_http, ConversationClient, UserTokenClient, ServiceUrl, mergedRequestContext);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified inbound activity.
    /// </summary>
    /// <param name="activity">The inbound activity that supplies the service URL and request context.</param>
    /// <returns>A new <see cref="ApiClient"/> bound to the activity's service URL and request context.</returns>
    public virtual ApiClient ForActivity(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return ForRequestContext(BotRequestContext.FromInboundActivity(activity))
            .ForServiceUrl(activity.ServiceUrl ?? throw new InvalidOperationException("Activity.ServiceUrl is required to use the Api client."));
    }
}
