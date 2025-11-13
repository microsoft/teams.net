// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public class ClientCredentials(IAuthorizationHeaderProvider authorizationHeaderProvider) : IHttpCredentials
{
    public async Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, CancellationToken cancellationToken = default)
    {
        AuthorizationHeaderProviderOptions options = new();
        options.AcquireTokenOptions = new AcquireTokenOptions()
        {
            AuthenticationOptionsName = "AzureAd",
        };
        var tokenResult = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(scopes[0], options, cancellationToken);
        return new TokenResponse
        {
            AccessToken = tokenResult.Substring("Bearer ".Length),
            TokenType = "Bearer",
        };
    }
}