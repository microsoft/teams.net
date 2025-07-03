// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotTokenClient : Client
{
    public BotTokenClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {

    }

    public BotTokenClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {

    }

    public BotTokenClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {

    }

    public BotTokenClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {

    }

    public async Task<ITokenResponse> GetAsync(IHttpCredentials credentials)
    {
        return await credentials.Resolve(_http, ["https://api.botframework.com/.default"], _cancellationToken);
    }

    public async Task<ITokenResponse> GetGraphAsync(IHttpCredentials credentials)
    {
        return await credentials.Resolve(_http, ["https://graph.microsoft.com/.default"], _cancellationToken);
    }
}