// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class UserClient : Client
{
    public UserTokenClient Token { get; }
    private readonly ApiClientOptions _apiClientOptions;

    public UserClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }

    public UserClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }

    public UserClient(IHttpClient client, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, ApiClientOptions? apiClientOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiClientOptions);
        Token = new UserTokenClient(_http, _apiClientOptions, cancellationToken);
    }
}