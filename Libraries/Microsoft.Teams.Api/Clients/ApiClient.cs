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
    private readonly ApiClientOptions _options;

    public ApiClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge(apiOptions);
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge(apiOptions);
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _options = ApiClientOptions.Merge(apiOptions);
        Bots = new BotClient(_http, _options, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _options, cancellationToken);
        Users = new UserClient(_http, _options, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _options, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _options, cancellationToken);
    }

    public ApiClient(ApiClient client) : base()
    {
        ServiceUrl = client.ServiceUrl;
        _options = client._options;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
        _cancellationToken = client._cancellationToken;
    }
}