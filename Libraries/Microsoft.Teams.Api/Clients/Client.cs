// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Common.Http;

using IHttpClientFactory = Microsoft.Teams.Common.Http.IHttpClientFactory;
namespace Microsoft.Teams.Api.Clients;

public abstract class Client
{
    protected IHttpClient _http;
    protected CancellationToken _cancellationToken;
    public string? Scope { get; set; }

    public AgenticIdentity? AgenticIdentity { get; set; }

    public Client(CancellationToken cancellationToken = default)
    {
        _http = new Common.Http.HttpClient();
        _cancellationToken = cancellationToken;
    }

    public Client(IHttpClient client, string scope, CancellationToken cancellationToken = default)
    {
        _http = client;
        Scope = scope;
        _cancellationToken = cancellationToken;
    }

    public Client(IHttpClientOptions options, CancellationToken cancellationToken = default)
    {
        _http = new Common.Http.HttpClient(options);
        _cancellationToken = cancellationToken;
    }

    public Client(IHttpClientFactory factory, CancellationToken cancellationToken = default)
    {
        _http = factory.CreateClient("default");
        _cancellationToken = cancellationToken;
    }
}