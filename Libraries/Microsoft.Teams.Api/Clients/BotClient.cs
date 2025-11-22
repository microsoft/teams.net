// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

using IHttpClientFactory = Microsoft.Teams.Common.Http.IHttpClientFactory;
namespace Microsoft.Teams.Api.Clients;

public class BotClient : Client
{
    public virtual BotTokenClient Token { get; }
    public BotSignInClient SignIn { get; }

    public BotClient() : this(default!)
    {

    }

    public BotClient(string scope, CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        Token = new BotTokenClient(_http, scope, cancellationToken);
        SignIn = new BotSignInClient(_http, scope, cancellationToken);
    }

    public BotClient(IHttpClient client, string scope, CancellationToken cancellationToken = default) : base(client, scope, cancellationToken)
    {
        Token = new BotTokenClient(_http, scope, cancellationToken);
        SignIn = new BotSignInClient(_http, scope, cancellationToken);
    }

    public BotClient(IHttpClientOptions options, string scope, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        Token = new BotTokenClient(_http, scope, cancellationToken);
        SignIn = new BotSignInClient(_http, scope, cancellationToken);
    }

    public BotClient(IHttpClientFactory factory, string scope, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        Token = new BotTokenClient(_http, scope, cancellationToken);
        SignIn = new BotSignInClient(_http, scope, cancellationToken);
    }
}