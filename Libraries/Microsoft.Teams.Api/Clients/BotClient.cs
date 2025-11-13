// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotClient : Client
{
    public virtual BotTokenClient Token { get; }
    public BotSignInClient SignIn { get; }
    private readonly ApiClientOptions _apiClientSettings;

    public BotClient() : this(default)
    {

    }

    public BotClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }

    public BotClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }

    public BotClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }

    public BotClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge();
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }

    public BotClient(IHttpClient client, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }

    public BotClient(IHttpClientOptions options, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }

    public BotClient(IHttpClientFactory factory, ApiClientOptions? apiClientSettings, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        _apiClientSettings = ApiClientOptions.Merge(apiClientSettings);
        Token = new BotTokenClient(_http, _apiClientSettings, cancellationToken);
        SignIn = new BotSignInClient(_http, _apiClientSettings, cancellationToken);
    }
}