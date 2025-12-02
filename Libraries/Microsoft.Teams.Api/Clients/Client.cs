// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public abstract class Client
{
    protected ICustomHttpClient _http;
    protected CancellationToken _cancellationToken;

    public Client(CancellationToken cancellationToken = default)
    {
        _http = new Common.Http.HttpClient();
        _cancellationToken = cancellationToken;
    }

    public Client(ICustomHttpClient client, CancellationToken cancellationToken = default)
    {
        _http = client;
        _cancellationToken = cancellationToken;
    }

    public Client(IHttpClientOptions options, CancellationToken cancellationToken = default)
    {
        _http = new Common.Http.HttpClient(options);
        _cancellationToken = cancellationToken;
    }

    public Client(ICustomHttpClientFactory factory, CancellationToken cancellationToken = default)
    {
        _http = factory.CreateClient("default");
        _cancellationToken = cancellationToken;
    }
}