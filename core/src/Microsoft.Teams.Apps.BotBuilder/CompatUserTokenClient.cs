// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Provides a compatibility layer that adapts the Teams Bot Core <see cref="UserTokenClient"/> to the Bot Framework's
/// <see cref="Microsoft.Bot.Connector.Authentication.UserTokenClient"/> interface.
/// </summary>
/// <remarks>
/// This adapter enables legacy Bot Framework bots to use the new Teams Bot Core token management system
/// without code changes. It converts between the two different token result formats and delegates all operations
/// to the underlying Core UserTokenClient.
/// </remarks>
/// <param name="utc">The underlying Teams Bot Core UserTokenClient that performs the actual token operations.</param>
internal sealed class CompatUserTokenClient(UserTokenClient utc) : Microsoft.Bot.Connector.Authentication.UserTokenClient
{
    /// <summary>
    /// Gets the status of all tokens for a specific user across all configured OAuth connections.
    /// </summary>
    /// <param name="userId">The unique identifier of the user. Cannot be null or empty.</param>
    /// <param name="channelId">The channel identifier where the user is interacting. Cannot be null or empty.</param>
    /// <param name="includeFilter">Optional filter to limit which token statuses are returned. Pass null or empty to include all.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an array of <see cref="TokenStatus"/>
    /// objects representing the status of each configured connection for the user.
    /// </returns>
    public async override Task<TokenStatus[]> GetTokenStatusAsync(string userId, string channelId, string includeFilter, CancellationToken cancellationToken)
    {
        GetTokenStatusResult[] res = await utc.GetTokenStatusAsync(userId, channelId, includeFilter, cancellationToken).ConfigureAwait(false);
        return res.Select(t => new TokenStatus
        {
            ChannelId = channelId,
            ConnectionName = t.ConnectionName,
            HasToken = t.HasToken,
            ServiceProviderDisplayName = t.ServiceProviderDisplayName,
        }).ToArray();
    }

    /// <summary>
    /// Retrieves an OAuth token for a user from a specific connection.
    /// </summary>
    /// <param name="userId">The unique identifier of the user requesting the token. Cannot be null or empty.</param>
    /// <param name="connectionName">The name of the OAuth connection configured in Azure Bot Service. Cannot be null or empty.</param>
    /// <param name="channelId">The channel identifier where the user is interacting. Cannot be null or empty.</param>
    /// <param name="magicCode">Optional magic code from the OAuth callback. Used to complete the OAuth flow when provided.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="TokenResponse"/> with
    /// the OAuth token if available, or null if the user has not completed authentication for this connection.
    /// </returns>
    public async override Task<TokenResponse?> GetUserTokenAsync(string userId, string connectionName, string channelId, string magicCode, CancellationToken cancellationToken)
    {
        GetTokenResult? res = await utc.GetTokenAsync(userId, connectionName, channelId, magicCode, cancellationToken).ConfigureAwait(false);
        if (res == null)
        {
            return null;
        }

        return new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = res.ConnectionName,
            Token = res.Token
        };
    }

    /// <summary>
    /// Retrieves the sign-in resource (URL and exchange resources) needed to initiate an OAuth flow for a user.
    /// </summary>
    /// <param name="connectionName">The name of the OAuth connection configured in Azure Bot Service. Cannot be null or empty.</param>
    /// <param name="activity">The activity associated with the sign-in request. Used to extract user and channel information. Cannot be null.</param>
    /// <param name="finalRedirect">Optional URL to redirect the user to after completing authentication.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="SignInResource"/>
    /// with the sign-in link and optional token exchange or post resources for completing the OAuth flow.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="activity"/> is null.</exception>
    public async override Task<SignInResource> GetSignInResourceAsync(string connectionName, Activity activity, string finalRedirect, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        GetSignInResourceResult res = await utc.GetSignInResource(activity.From.Id, connectionName, activity.ChannelId, finalRedirect, cancellationToken).ConfigureAwait(false);
        SignInResource signInResource = new()
        {
            SignInLink = res!.SignInLink
        };

        if (res.TokenExchangeResource != null)
        {
            signInResource.TokenExchangeResource = new Microsoft.Bot.Schema.TokenExchangeResource
            {
                Id = res.TokenExchangeResource.Id,
                Uri = res.TokenExchangeResource.Uri?.ToString(),
                ProviderId = res.TokenExchangeResource.ProviderId
            };
        }

        if (res.TokenPostResource != null)
        {
            signInResource.TokenPostResource = new Microsoft.Bot.Schema.TokenPostResource
            {
                SasUrl = res.TokenPostResource.SasUrl?.ToString()
            };
        }

        return signInResource;
    }

    /// <summary>
    /// Exchanges a token from one OAuth connection for a token from another connection using single sign-on (SSO).
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose token is being exchanged. Cannot be null or empty.</param>
    /// <param name="connectionName">The name of the target OAuth connection to exchange to. Cannot be null or empty.</param>
    /// <param name="channelId">The channel identifier where the user is interacting. Cannot be null or empty.</param>
    /// <param name="exchangeRequest">The token exchange request containing the source token. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="TokenResponse"/>
    /// with the exchanged token for the target connection.
    /// </returns>
    public async override Task<TokenResponse> ExchangeTokenAsync(string userId, string connectionName, string channelId,
     TokenExchangeRequest exchangeRequest, CancellationToken cancellationToken)
    {
        GetTokenResult resp = await utc.ExchangeTokenAsync(userId, connectionName, channelId, exchangeRequest.Token,
        cancellationToken).ConfigureAwait(false);
        return new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = resp.ConnectionName,
            Token = resp.Token
        };
    }

    /// <summary>
    /// Signs out a user from a specific OAuth connection, revoking their stored token.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to sign out. Cannot be null or empty.</param>
    /// <param name="connectionName">The name of the OAuth connection to sign out from. Cannot be null or empty.</param>
    /// <param name="channelId">The channel identifier where the user is interacting. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous sign-out operation.</returns>
    public async override Task SignOutUserAsync(string userId, string connectionName, string channelId, CancellationToken cancellationToken)
    {
        await utc.SignOutUserAsync(userId, connectionName, channelId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves Azure Active Directory (Azure AD) tokens for multiple resource URLs in a single request.
    /// </summary>
    /// <param name="userId">The unique identifier of the user requesting the tokens. Cannot be null or empty.</param>
    /// <param name="connectionName">The name of the OAuth connection configured for Azure AD. Cannot be null or empty.</param>
    /// <param name="resourceUrls">An array of resource URLs (e.g., "https://graph.microsoft.com") to request tokens for. Cannot be null.</param>
    /// <param name="channelId">The channel identifier where the user is interacting. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a dictionary mapping each
    /// resource URL to its corresponding <see cref="TokenResponse"/>. Returns an empty dictionary if no tokens are available.
    /// </returns>
    public async override Task<Dictionary<string, TokenResponse>> GetAadTokensAsync(string userId, string connectionName, string[] resourceUrls, string channelId, CancellationToken cancellationToken)
    {
        IDictionary<string, GetTokenResult> res = await utc.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls, cancellationToken).ConfigureAwait(false);
        return res?.ToDictionary(kvp => kvp.Key, kvp => new TokenResponse
        {
            ChannelId = channelId,
            ConnectionName = kvp.Value.ConnectionName,
            Token = kvp.Value.Token
        }) ?? new Dictionary<string, TokenResponse>();
    }
}
