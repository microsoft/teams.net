// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER

using Azure.Core;
using Azure.Identity;

using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Api.Auth;

/// <summary>
/// Credentials that use Azure Managed Identity for authentication.
/// Supports both System-Assigned and User-Assigned Managed Identities.
/// </summary>
public class ManagedIdentityCredentials : IHttpCredentials
{
    private readonly TokenCredential _credential;
    private readonly string[] _defaultScopes = ["https://api.botframework.com/.default"];

    /// <summary>
    /// Creates credentials using System-Assigned Managed Identity.
    /// </summary>
    public ManagedIdentityCredentials()
    {
        _credential = new ManagedIdentityCredential();
    }

    /// <summary>
    /// Creates credentials using User-Assigned Managed Identity with the specified client ID.
    /// </summary>
    /// <param name="clientId">The client ID of the user-assigned managed identity.</param>
    public ManagedIdentityCredentials(string clientId)
    {
        _credential = new ManagedIdentityCredential(clientId);
    }

    /// <summary>
    /// Creates credentials using a custom TokenCredential implementation.
    /// This allows using other Azure.Identity credential types like DefaultAzureCredential.
    /// </summary>
    /// <param name="credential">The TokenCredential to use for authentication.</param>
    public ManagedIdentityCredentials(TokenCredential credential)
    {
        _credential = credential;
    }

    /// <summary>
    /// Resolves the credentials to an access token.
    /// </summary>
    /// <param name="client">The HTTP client (not used in this implementation).</param>
    /// <param name="scopes">The scopes to request. If empty, defaults to botframework.com scope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response containing the access token.</returns>
    public async Task<ITokenResponse> Resolve(IHttpClient client, string[] scopes, CancellationToken cancellationToken = default)
    {
        var scopesToUse = scopes.Length > 0 ? scopes : _defaultScopes;

        var tokenRequestContext = new TokenRequestContext(scopesToUse);
        var accessToken = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);

        return new ManagedIdentityTokenResponse(accessToken.Token, accessToken.ExpiresOn);
    }

    private class ManagedIdentityTokenResponse : ITokenResponse
    {
        public string TokenType => "Bearer";
        public int? ExpiresIn { get; }
        public string AccessToken { get; }

        public ManagedIdentityTokenResponse(string accessToken, DateTimeOffset expiresOn)
        {
            AccessToken = accessToken;
            ExpiresIn = (int)(expiresOn - DateTimeOffset.UtcNow).TotalSeconds;
        }
    }
}

#endif