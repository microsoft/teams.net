// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;

using CoreUserTokenClient = Microsoft.Teams.Core.UserTokenClient;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for user token operations.
/// Delegates to the core <see cref="CoreUserTokenClient"/>.
/// </summary>
public class UserTokenApiClient
{
    private readonly CoreUserTokenClient _client;

    internal UserTokenApiClient(CoreUserTokenClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Get a user token for a connection.
    /// </summary>
    public Task<GetTokenResult?> GetAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
    {
        return _client.GetTokenAsync(userId, connectionName, channelId, code, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get AAD tokens for specified resources.
    /// </summary>
    public async Task<IDictionary<string, GetTokenResult>?> GetAadAsync(string userId, string connectionName, string channelId, IList<string>? resourceUrls = null, CancellationToken cancellationToken = default)
    {
        return await _client.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls?.ToArray(), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the token status for a user's connections.
    /// </summary>
    public async Task<IList<GetTokenStatusResult>?> GetStatusAsync(string userId, string channelId, string? includeFilter = null, CancellationToken cancellationToken = default)
    {
        return await _client.GetTokenStatusAsync(userId, channelId, includeFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sign a user out of a connection.
    /// </summary>
    public Task SignOutAsync(string userId, string connectionName, string channelId, CancellationToken cancellationToken = default)
    {
        return _client.SignOutUserAsync(userId, connectionName, channelId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Exchange a token for another token.
    /// </summary>
    public async Task<GetTokenResult?> ExchangeAsync(string userId, string connectionName, string channelId, string exchangeToken, CancellationToken cancellationToken = default)
    {
        return await _client.ExchangeTokenAsync(userId, connectionName, channelId, exchangeToken, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the sign-in URL for a connection.
    /// </summary>
    public Task<string?> GetSignInUrlAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInUrlAsync(state, codeChallenge, emulatorUrl, finalRedirect, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get the sign-in resource for a connection.
    /// </summary>
    public Task<GetSignInResourceResult> GetSignInResourceAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInResourceAsync(state, codeChallenge, emulatorUrl, finalRedirect, cancellationToken: cancellationToken);
    }
}
