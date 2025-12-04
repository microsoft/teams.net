// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

using IHttpClientFactory = Microsoft.Teams.Common.Http.IHttpClientFactory;
namespace Microsoft.Teams.Api.Clients;

public class UserClient : Client
{
    public UserTokenClient Token { get; }

    public UserClient(string scope,CancellationToken cancellationToken = default) : base(cancellationToken)
    {
        Token = new UserTokenClient(_http, scope, cancellationToken);
    }

    public UserClient(IHttpClient client, string scope, CancellationToken cancellationToken = default) : base(client,scope, cancellationToken)
    {
        Token = new UserTokenClient(_http, scope, cancellationToken);
    }

    public UserClient(IHttpClientOptions options, string scope, CancellationToken cancellationToken = default) : base(options, cancellationToken)
    {
        Token = new UserTokenClient(_http, scope, cancellationToken);
    }

    public UserClient(IHttpClientFactory factory, string scope, CancellationToken cancellationToken = default) : base(factory, cancellationToken)
    {
        Token = new UserTokenClient(_http, scope, cancellationToken);
    }
}