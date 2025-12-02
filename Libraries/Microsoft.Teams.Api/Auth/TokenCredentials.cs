// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public delegate Task<ITokenResponse> TokenFactory(string? tenantId, params string[] scopes);

public class TokenCredentials : IHttpCredentials
{
    public string ClientId { get; set; }
    public string? TenantId { get; set; }
    public TokenFactory Token { get; set; }

    public TokenCredentials(string clientId, TokenFactory token)
    {
        ClientId = clientId;
        Token = token;
    }

    public TokenCredentials(string clientId, string tenantId, TokenFactory token)
    {
        ClientId = clientId;
        TenantId = tenantId;
        Token = token;
    }

    public async Task<ITokenResponse> Resolve(ICustomHttpClient _client, string[] scopes, CancellationToken cancellationToken = default)
    {
        return await Token(TenantId, scopes);
    }
}