// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class UserClient : Client
{
    public UserTokenClient Token { get; }
    private readonly ApiClientOptions _options;

    public UserClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _options = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }

    public UserClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _options = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _options = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _options = ApiClientOptions.Merge();
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }

    public UserClient(IHttpClient client, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _options = ApiClientOptions.Merge(apiOptions);
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _options = ApiClientOptions.Merge(apiOptions);
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _options = ApiClientOptions.Merge(apiOptions);
        Token = new UserTokenClient(_http, _options, cancellationToken);
    }
}