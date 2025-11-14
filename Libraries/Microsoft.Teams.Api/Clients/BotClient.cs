// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotClient : Client
{
    public virtual BotTokenClient Token { get; }
    public BotSignInClient SignIn { get; }
    private readonly ApiClientOptions _apiClientOptions;

    public BotClient() : this(default)
    {

    }

    public BotClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }

    public BotClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }

    public BotClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }

    public BotClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }

    public BotClient(IHttpClient client, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiOptions);
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }

    public BotClient(IHttpClientOptions options, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiOptions);
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }

    public BotClient(IHttpClientFactory factory, ApiClientOptions? apiOptions, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientOptions = ApiClientOptions.Merge(apiOptions);
        Token = new BotTokenClient(_http, _apiClientOptions, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientOptions, cancellationToken);
    }
}