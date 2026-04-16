// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core.Http;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Top-level API client that provides access to all Teams Bot API sub-clients.
/// </summary>
public class ApiClient
{
    private readonly BotHttpClient _http;

    /// <summary>
    /// The service URL used by this client.
    /// </summary>
    public Uri ServiceUrl { get; }

    /// <summary>
    /// Client for bot-level operations (token, sign-in).
    /// </summary>
    public BotClient Bots { get; }

    /// <summary>
    /// Client for conversation operations (activities, members, reactions).
    /// </summary>
    public V3ConversationClient Conversations { get; }

    /// <summary>
    /// Client for user-level operations (token).
    /// </summary>
    public UserClient Users { get; }

    /// <summary>
    /// Client for team operations.
    /// </summary>
    public TeamClient Teams { get; }

    /// <summary>
    /// Client for meeting operations.
    /// </summary>
    public MeetingClient Meetings { get; }

    /// <summary>
    /// Creates a new <see cref="ApiClient"/> instance.
    /// </summary>
    /// <param name="serviceUrl">The Bot Framework service URL.</param>
    /// <param name="httpClient">An <see cref="HttpClient"/> configured with authentication (e.g., via DI with <c>BotAuthenticationHandler</c>).</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="tokenApiEndpoint">Optional token API endpoint override. Defaults to https://token.botframework.com.</param>
    public ApiClient(Uri serviceUrl, HttpClient httpClient, ILogger? logger = null, string tokenApiEndpoint = "https://token.botframework.com")
    {
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(httpClient);

        string serviceUrlString = serviceUrl.ToString();
        ServiceUrl = serviceUrl;
        _http = new BotHttpClient(httpClient, logger);
        Bots = new BotClient(_http, tokenApiEndpoint);
        Conversations = new V3ConversationClient(serviceUrlString, _http);
        Users = new UserClient(_http, tokenApiEndpoint);
        Teams = new TeamClient(serviceUrlString, _http);
        Meetings = new MeetingClient(serviceUrlString, _http);
    }

    /// <summary>
    /// Creates a copy of an existing <see cref="ApiClient"/> with the same configuration.
    /// </summary>
    public ApiClient(ApiClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        ServiceUrl = client.ServiceUrl;
        _http = client._http;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
    }
}
