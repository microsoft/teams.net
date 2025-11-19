// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

public class ClientCredentials(IAuthorizationHeaderProvider authorizationHeaderProvider) : IHttpCredentials
{
    public async Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, AgenticIdentity agenticIdentity, CancellationToken cancellationToken = default)
    {
        AuthorizationHeaderProviderOptions options = new();

        string tokenResult;
        if (scopes.Contains("https://api.botframework.com/.default"))
        {
            tokenResult = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(scopes[0], options, cancellationToken);
        }
        else
        {
            if (agenticIdentity is not null)
            {
                options.WithAgentUserIdentity(agenticIdentity.AgentticAppId!, Guid.Parse(agenticIdentity.AgenticUserId!));
            }
            tokenResult = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(scopes, options, null, cancellationToken);
        }


        return new TokenResponse
        {
            AccessToken = tokenResult.Substring("Bearer ".Length),
            TokenType = "Bearer",
        };
    }
}