// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Clients;

public class UserClient : Client
{
    public UserTokenClient Token { get; }

    public UserClient(CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        Token = new UserTokenClient(_http, cancellationToken);
    }

    public UserClient(IHttpClient client, CancellationToken cancellationToken = default) : base(client, cancellationToken)
    {
        Token = new UserTokenClient(_http, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        Token = new UserTokenClient(_http, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        Token = new UserTokenClient(_http, cancellationToken);
    }
}