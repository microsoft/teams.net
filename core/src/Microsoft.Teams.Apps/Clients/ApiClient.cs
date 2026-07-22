// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

using CoreConversationClient = Microsoft.Teams.Core.ConversationClient;
using CoreUserTokenClient = Microsoft.Teams.Core.UserTokenClient;

namespace Microsoft.Teams.Apps.Clients;

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
/// <item><b>Fully initialized</b> — Use the internal service-URL constructor
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
    /// The agentic identity used by this client for all operations, or null.
    /// Set once at the client level (like <see cref="ServiceUrl"/>) via <see cref="ForAgenticIdentity"/>
    /// rather than per method call.
    /// </summary>
    public virtual AgenticIdentity? AgenticIdentity { get; }

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
    /// Use <see cref="ForServiceUrl"/> to create a scoped instance bound to a specific service URL.
    /// </summary>
    /// <param name="httpClient">An <see cref="HttpClient"/> configured with authentication (e.g., via DI with <see cref="Microsoft.Teams.Core.Hosting.BotAuthenticationHandler"/>).</param>
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
        AgenticIdentity = null;
        Conversations = null!;
        Teams = null!;
        Meetings = null!;
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> bound to a specific service URL.
    /// </summary>
    /// <param name="serviceUrl">The Bot Framework service URL.</param>
    /// <param name="httpClient">An <see cref="HttpClient"/> configured with authentication (e.g., via DI with <see cref="Microsoft.Teams.Core.Hosting.BotAuthenticationHandler"/>).</param>
    /// <param name="conversationClient">The core conversation client for conversation/activity/member operations.</param>
    /// <param name="userTokenClient">The core user token client for sign-in and token operations.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="agenticIdentity">Optional agentic identity used for all operations on this client.</param>
    internal ApiClient(Uri serviceUrl, HttpClient httpClient, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, ILogger? logger = null, AgenticIdentity? agenticIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(conversationClient);
        ArgumentNullException.ThrowIfNull(userTokenClient);

        _http = new BotHttpClient(httpClient, logger);
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        ServiceUrl = serviceUrl;
        AgenticIdentity = agenticIdentity;
        Conversations = new ConversationApiClient(serviceUrl, conversationClient, agenticIdentity);
        Teams = new TeamClient(serviceUrl.ToString(), _http, agenticIdentity);
        Meetings = new MeetingClient(serviceUrl.ToString(), _http, agenticIdentity);
    }

    // Private constructor for ForServiceUrl/ForAgenticIdentity — shares BotHttpClient, ConversationClient, and UserTokenClient
    private ApiClient(BotHttpClient http, CoreConversationClient conversationClient, CoreUserTokenClient userTokenClient, Uri serviceUrl, AgenticIdentity? agenticIdentity)
    {
        _http = http;
        ConversationClient = conversationClient;
        UserTokenClient = userTokenClient;
        ServiceUrl = serviceUrl;
        AgenticIdentity = agenticIdentity;
        Conversations = new ConversationApiClient(serviceUrl, conversationClient, agenticIdentity);
        Teams = new TeamClient(serviceUrl.ToString(), http, agenticIdentity);
        Meetings = new MeetingClient(serviceUrl.ToString(), http, agenticIdentity);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified service URL,
    /// sharing the underlying HTTP client, authentication, and agentic identity.
    /// </summary>
    /// <param name="serviceUrl">The Bot Framework service URL for this scope.</param>
    /// <returns>A new <see cref="ApiClient"/> bound to the given service URL.</returns>
    public virtual ApiClient ForServiceUrl(Uri serviceUrl)
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        return new ApiClient(_http, ConversationClient, UserTokenClient, serviceUrl, AgenticIdentity);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to the specified agentic identity,
    /// used for all operations on the returned client (like <see cref="ServiceUrl"/>).
    /// </summary>
    /// <param name="agenticIdentity">The agentic identity to authenticate as, or null.</param>
    /// <returns>A new <see cref="ApiClient"/> bound to the given agentic identity.</returns>
    public virtual ApiClient ForAgenticIdentity(AgenticIdentity? agenticIdentity)
    {
        return new ApiClient(_http, ConversationClient, UserTokenClient, ServiceUrl, agenticIdentity);
    }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> scoped to an inbound activity, binding both the
    /// service URL and the agentic identity (from the activity's <see cref="CoreActivity.Recipient"/>) in one call.
    /// </summary>
    /// <param name="activity">The inbound activity to derive routing and identity from.</param>
    /// <returns>A new <see cref="ApiClient"/> bound to the activity's service URL and agentic identity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity has no service URL.</exception>
    public virtual ApiClient ForActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        Uri serviceUrl = activity.ServiceUrl
            ?? throw new InvalidOperationException("Activity.ServiceUrl is required to create a scoped API client.");
        return new ApiClient(_http, ConversationClient, UserTokenClient, serviceUrl, activity.Recipient?.GetAgenticIdentity());
    }
}
