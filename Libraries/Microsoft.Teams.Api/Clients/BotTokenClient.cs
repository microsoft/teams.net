// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotTokenClient : Client
{
    public static readonly string BotScope = "https://api.botframework.com/.default";
    public static readonly string GraphScope = "https://graph.microsoft.com/.default";

    private readonly ApiClientOptions _apiClientSettings;

    public BotTokenClient() : this(default)
    {

    }

    public BotTokenClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public BotTokenClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public BotTokenClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public BotTokenClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
    }

    public BotTokenClient(IHttpClient client, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
    }

    public BotTokenClient(IHttpClientOptions options, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
    }

    public BotTokenClient(IHttpClientFactory factory, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
    }

    public virtual async Task<ITokenResponse> GetAsync(IHttpCredentials credentials, IHttpClient? http = null)
    {
        return await credentials.Resolve(http ?? _http, [BotScope], _cancellationToken);
    }

    public async Task<ITokenResponse> GetGraphAsync(IHttpCredentials credentials, IHttpClient? http = null)
    {
        return await credentials.Resolve(http ?? _http, [GraphScope], _cancellationToken);
    }
}