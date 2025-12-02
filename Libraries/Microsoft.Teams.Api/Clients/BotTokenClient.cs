// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotTokenClient : Client
{
    public static readonly string BotScope = "https://api.botframework.com/.default";
    public static readonly string GraphScope = "https://graph.microsoft.com/.default";

    public BotTokenClient() : this(default)
    {

    }

    public BotTokenClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {

    }

    public BotTokenClient(ICustomHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {

    }

    public BotTokenClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {

    }

    public BotTokenClient(ICustomHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {

    }

    public virtual async Task<ITokenResponse> GetAsync(IHttpCredentials credentials, ICustomHttpClient? http = null)
    {
        return await credentials.Resolve(http ?? _http, [BotScope], _cancellationToken);
    }

    public async Task<ITokenResponse> GetGraphAsync(IHttpCredentials credentials, ICustomHttpClient? http = null)
    {
        return await credentials.Resolve(http ?? _http, [GraphScope], _cancellationToken);
    }
}