// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps.Api;

/// <summary>
/// Provides user token operations for OAuth SSO.
/// </summary>
public class UserTokenApi
{
    private readonly UserTokenClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserTokenApi"/> class.
    /// </summary>
    /// <param name="userTokenClient">The user token client for token operations.</param>
    internal UserTokenApi(UserTokenClient userTokenClient)
    {
        _client = userTokenClient;
    }

    /// <summary>
    /// Gets the user token for a particular connection.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="code">The optional authorization code.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the token result, or null if no token is available.</returns>
    public Task<GetTokenResult?> GetAsync(
        string userId,
        string connectionName,
        string channelId,
        string? code = null,
        CancellationToken cancellationToken = default)
        => _client.GetTokenAsync(userId, connectionName, channelId, code, cancellationToken);

    /// <summary>
    /// Gets the user token for a particular connection using activity context.
    /// </summary>
    /// <param name="activity">The activity providing user context. Must contain valid From.Id and ChannelId.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="code">The optional authorization code.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the token result, or null if no token is available.</returns>
    public Task<GetTokenResult?> GetAsync(
        TeamsActivity activity,
        string connectionName,
        string? code = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetTokenAsync(
            activity.From.Id!,
            connectionName,
            activity.ChannelId!,
            code,
            cancellationToken);
    }

    /// <summary>
    /// Exchanges a token for another token.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="exchangeToken">The token to exchange.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the exchanged token.</returns>
    public Task<GetTokenResult> ExchangeAsync(
        string userId,
        string connectionName,
        string channelId,
        string? exchangeToken,
        CancellationToken cancellationToken = default)
        => _client.ExchangeTokenAsync(userId, connectionName, channelId, exchangeToken, cancellationToken);

    /// <summary>
    /// Exchanges a token for another token using activity context.
    /// </summary>
    /// <param name="activity">The activity providing user context. Must contain valid From.Id and ChannelId.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="exchangeToken">The token to exchange.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the exchanged token.</returns>
    public Task<GetTokenResult> ExchangeAsync(
        TeamsActivity activity,
        string connectionName,
        string? exchangeToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.ExchangeTokenAsync(
            activity.From.Id!,
            connectionName,
            activity.ChannelId!,
            exchangeToken,
            cancellationToken);
    }

    /// <summary>
    /// Signs the user out of a connection, revoking their OAuth token.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to sign out.</param>
    /// <param name="connectionName">Optional name of the OAuth connection to sign out from. If null, signs out from all connections.</param>
    /// <param name="channelId">Optional channel identifier. If provided, limits sign-out to tokens for this channel.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous sign-out operation.</returns>
    public Task SignOutAsync(
        string userId,
        string? connectionName = null,
        string? channelId = null,
        CancellationToken cancellationToken = default)
        => _client.SignOutUserAsync(userId, connectionName, channelId, cancellationToken);

    /// <summary>
    /// Signs the user out of a connection using activity context.
    /// </summary>
    /// <param name="activity">The activity providing user context. Must contain valid From.Id.</param>
    /// <param name="connectionName">Optional name of the OAuth connection to sign out from. If null, signs out from all connections.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous sign-out operation.</returns>
    public Task SignOutAsync(
        TeamsActivity activity,
        string? connectionName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.SignOutUserAsync(
            activity.From.Id!,
            connectionName,
            activity.ChannelId,
            cancellationToken);
    }

    /// <summary>
    /// Gets AAD tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="resourceUrls">The resource URLs to get tokens for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of resource URLs to token results.</returns>
    public Task<IDictionary<string, GetTokenResult>> GetAadTokensAsync(
        string userId,
        string connectionName,
        string channelId,
        string[]? resourceUrls = null,
        CancellationToken cancellationToken = default)
        => _client.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls, cancellationToken);

    /// <summary>
    /// Gets AAD tokens for a user using activity context.
    /// </summary>
    /// <param name="activity">The activity providing user context. Must contain valid From.Id and ChannelId.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="resourceUrls">The resource URLs to get tokens for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of resource URLs to token results.</returns>
    public Task<IDictionary<string, GetTokenResult>> GetAadTokensAsync(
        TeamsActivity activity,
        string connectionName,
        string[]? resourceUrls = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetAadTokensAsync(
            activity.From.Id!,
            connectionName,
            activity.ChannelId!,
            resourceUrls,
            cancellationToken);
    }

    /// <summary>
    /// Gets the token status for each connection for the given user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="include">The optional include parameter.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of token status results.</returns>
    public Task<GetTokenStatusResult[]> GetStatusAsync(
        string userId,
        string channelId,
        string? include = null,
        CancellationToken cancellationToken = default)
        => _client.GetTokenStatusAsync(userId, channelId, include, cancellationToken);

    /// <summary>
    /// Gets the token status for each connection using activity context.
    /// </summary>
    /// <param name="activity">The activity providing user context. Must contain valid From.Id and ChannelId.</param>
    /// <param name="include">The optional include parameter.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of token status results.</returns>
    public Task<GetTokenStatusResult[]> GetStatusAsync(
        TeamsActivity activity,
        string? include = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetTokenStatusAsync(
            activity.From.Id!,
            activity.ChannelId!,
            include,
            cancellationToken);
    }

    /// <summary>
    /// Gets the sign-in resource for a user to authenticate via OAuth.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="finalRedirect">The optional final redirect URL after sign-in completes.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the sign-in resource with sign-in link and token exchange information.</returns>
    public Task<GetSignInResourceResult> GetSignInResourceAsync(
        string userId,
        string connectionName,
        string channelId,
        string? finalRedirect = null,
        CancellationToken cancellationToken = default)
        => _client.GetSignInResource(userId, connectionName, channelId, finalRedirect, cancellationToken);

    /// <summary>
    /// Gets the sign-in resource for a user to authenticate via OAuth using activity context.
    /// </summary>
    /// <param name="activity">The activity providing user context. Must contain valid From.Id and ChannelId.</param>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="finalRedirect">The optional final redirect URL after sign-in completes.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the sign-in resource with sign-in link and token exchange information.</returns>
    public Task<GetSignInResourceResult> GetSignInResourceAsync(
        TeamsActivity activity,
        string connectionName,
        string? finalRedirect = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return _client.GetSignInResource(
            activity.From.Id!,
            connectionName,
            activity.ChannelId!,
            finalRedirect,
            cancellationToken);
    }
}
