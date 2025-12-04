// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class MemberClient : Client
{
    public readonly string ServiceUrl;

    public MemberClient(string serviceUrl, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public MemberClient(string serviceUrl, IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public MemberClient(string serviceUrl, IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public MemberClient(string serviceUrl, IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        ServiceUrl = serviceUrl;
    }

    public async Task<List<TeamsChannelAccount>> GetAsync(string conversationId)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v3/conversations/{conversationId}/members");
        var response = await _http.SendAsync<List<TeamsChannelAccount>>(request, _cancellationToken);
        return response.Body;
    }

    public async Task<TeamsChannelAccount> GetByIdAsync(string conversationId, string memberId)
    {
        var request = HttpRequest.Get($"{ServiceUrl}v3/conversations/{conversationId}/members/{memberId}");
        var response = await _http.SendAsync<TeamsChannelAccount>(request, _cancellationToken);
        return response.Body;
    }

    public async Task DeleteAsync(string conversationId, string memberId)
    {
        var request = HttpRequest.Delete($"{ServiceUrl}v3/conversations/{conversationId}/members/{memberId}");
        await _http.SendAsync(request, _cancellationToken);
    }
}