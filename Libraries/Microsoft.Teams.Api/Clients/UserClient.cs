// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class UserClient : Client
{
    public UserTokenClient Token { get; }
    private readonly ApiClientOptions _apiClientSettings;

    public UserClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }

    public UserClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }

    public UserClient(IHttpClient client, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Token = new UserTokenClient(_http, _apiClientSettings, cancellationToken);
    }
}