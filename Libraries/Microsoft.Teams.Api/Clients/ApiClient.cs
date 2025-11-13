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
    private readonly ApiClientOptions _apiClientSettings;

    public ApiClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge();
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Bots = new BotClient(_http, _apiClientSettings, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Users = new UserClient(_http, _apiClientSettings, cancellationToken);
        Teams = new TeamClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
        Meetings = new MeetingClient(serviceUrl, _http, _apiClientSettings, cancellationToken);
    }

    public ApiClient(ApiClient client) : base()
    {
        ServiceUrl = client.ServiceUrl;
        _apiClientSettings = client._apiClientSettings;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        Teams = client.Teams;
        Meetings = client.Meetings;
        _cancellationToken = client._cancellationToken;
    }
}