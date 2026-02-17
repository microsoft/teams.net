// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class ApiClient : Client
{
    public virtual string ServiceUrl { get; }
    public virtual BotClient Bots { get; }
    public virtual ConversationClient Conversations { get; }
    public virtual UserClient Users { get; }
    public virtual TeamClient Teams { get; }
    public virtual MeetingClient Meetings { get; }

    /// <summary>
    /// Gets the underlying <see cref="IHttpClient"/> instance used by this <see cref="ApiClient"/>
    /// and its sub-clients to perform HTTP requests.
    /// </summary>
    /// <remarks>
    /// This property is provided for advanced scenarios where you need to issue custom HTTP
    /// calls that are not yet covered by the strongly-typed clients exposed by <see cref="ApiClient"/>.
    /// Prefer using the typed clients (<see cref="Bots"/>, <see cref="Conversations"/>,
    /// <see cref="Users"/>, <see cref="Teams"/>, <see cref="Meetings"/>) whenever possible.
    /// Relying on this property may couple your code to the current HTTP implementation and
    /// could limit future refactoring of the underlying client.
    /// </remarks>
    public IHttpClient Client { get => base._http; }

    public ApiClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(ApiClient client) : base()
    {
        ServiceUrl = client.ServiceUrl;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
        _cancellationToken = client._cancellationToken;
    }

    public ApiClient(ApiClient client, CancellationToken cancellationToken) : base(client._http, cancellationToken)
    {
        ServiceUrl = client.ServiceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(ServiceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(ServiceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(ServiceUrl, _http, cancellationToken);
    }
}