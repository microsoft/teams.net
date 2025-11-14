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
    private readonly ApiClientOptions _apiClientOptions;

    public ApiClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
        Bots = new BotClient(_http, _apiClientOptions, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Users = new UserClient(_http, _apiClientOptions, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientOptions, cancellationToken);
    }

    public ApiClient(ApiClient client) : base()
    {
        ServiceUrl = client.ServiceUrl;
        _apiClientOptions = client._apiClientOptions;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
        _cancellationToken = client._cancellationToken;
    }
}