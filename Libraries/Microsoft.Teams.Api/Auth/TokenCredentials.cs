// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public delegate Task<ITokenResponse> TokenFactory(string? tenantId, AgenticIdentity agenticIdentity, params string[] scopes);


/// <summary>
/// a factory for adding a token to http requests
/// </summary>
public delegate object? HttpTokenFactory();

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

    public async Task<ITokenResponse> Resolve(IHttpClient _client, string[] scopes, AgenticIdentity agenticIdentity, CancellationToken cancellationToken = default)
    {
        return await Token(TenantId, agenticIdentity, scopes);
    }
}