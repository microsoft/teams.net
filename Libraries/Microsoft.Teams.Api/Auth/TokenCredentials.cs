// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public delegate Task<ITokenResponse> TokenFactory(string? tenantId, params string[] scopes);

/// <summary>
/// Provide a <code>TokenFactory</code> that will be invoked whenever
/// the application needs a token.
/// TokenCredentials should be used with 3rd party packages like MSAL/Azure.Identity
/// to authenticate for any Federated/Managed Identity scenarios.
/// </summary>
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

    public async Task<ITokenResponse> Resolve(IHttpClient _, string[] scopes, CancellationToken cancellationToken = default)
    {
        return await Token(TenantId, scopes);
    }
}