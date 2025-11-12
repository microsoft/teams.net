// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
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

    public ApiClient(string serviceUrl, ILogger logger,  CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, logger, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, ILogger logger, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, logger, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, ILogger logger, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, logger, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, ILogger logger, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, logger, cancellationToken);
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
}