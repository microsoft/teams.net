// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class BotClient : Client
{
    public virtual BotTokenClient Token { get; }
    public BotSignInClient SignIn { get; }

    public BotClient() : this(default)
    {

    }

    public BotClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        Token = new BotTokenClient(_http, cancellationToken);
        SignIn = new BotSignInClient(_http, cancellationToken);
    }

    public BotClient(ICustomHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        Token = new BotTokenClient(_http, cancellationToken);
        SignIn = new BotSignInClient(_http, cancellationToken);
    }

    public BotClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        Token = new BotTokenClient(_http, cancellationToken);
        SignIn = new BotSignInClient(_http, cancellationToken);
    }

    public BotClient(ICustomHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        Token = new BotTokenClient(_http, cancellationToken);
        SignIn = new BotSignInClient(_http, cancellationToken);
    }
}