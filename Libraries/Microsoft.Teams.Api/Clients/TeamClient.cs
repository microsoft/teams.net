// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class TeamClient : Client
{
    public readonly string ServiceUrl;

    public TeamClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public TeamClient(string serviceUrl, ICustomHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public TeamClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public TeamClient(string serviceUrl, ICustomHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public async Task<Team> GetByIdAsync(string id)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v3/teams/{id}");
        var response = await _http.SendAsync<Team>(request, _cancellationToken);
        return response.Body;
    }

    public async Task<List<Channel>> GetConversationsAsync(string id)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v3/teams/{id}/conversations");
        var response = await _http.SendAsync<List<Channel>>(request, _cancellationToken);
        return response.Body;
    }
}