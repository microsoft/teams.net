﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class ApiClient : Client
{
    public virtual string ServiceUrl { get; }
    public virtual BotClient Bots { get; }
    public virtual ConversationClient Conversations { get; }
    public virtual UserClient Users { get; }

    public ApiClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
    }

    public ApiClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
        Bots = new BotClient(_http, cancellationToken);
        Conversations = new ConversationClient(serviceUrl, _http, cancellationToken);
        Users = new UserClient(_http, cancellationToken);
    }

    public ApiClient(ApiClient client) : base()
    {
        ServiceUrl = client.ServiceUrl;
        Bots = client.Bots;
        Conversations = client.Conversations;
        Users = client.Users;
        _cancellationToken = client._cancellationToken;
    }
}