// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

using CoreUserTokenClient = Microsoft.Teams.Core.UserTokenClient;

namespace Microsoft.Teams.Apps.Api.Clients;

/// <summary>
/// Client for user token operations.
/// Delegates to the core <see cref="CoreUserTokenClient"/>.
/// </summary>
public class UserTokenApiClient
{
    private readonly CoreUserTokenClient _client;
    private readonly BotRequestContext? _requestContext;

    internal UserTokenApiClient(CoreUserTokenClient client, BotRequestContext? requestContext = null)
    {
        _client = client;
        _requestContext = requestContext;
    }

    /// <summary>
    /// Get a user token for a connection.
    /// </summary>
    public Task<GetTokenResult?> GetAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
        => GetAsync(userId, connectionName, channelId, code, agenticIdentity: null, cancellationToken);

    /// <summary>
    /// Get a user token for a connection.
    /// </summary>
    public Task<GetTokenResult?> GetAsync(string userId, string connectionName, string channelId, string? code, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return _client.GetTokenAsync(userId, connectionName, channelId, code, CreateRequestContext(agenticIdentity), cancellationToken);
    }

    /// <summary>
    /// Get AAD tokens for specified resources.
    /// </summary>
    public async Task<IDictionary<string, GetTokenResult>?> GetAadAsync(string userId, string connectionName, string channelId, IList<string>? resourceUrls = null, CancellationToken cancellationToken = default)
        => await GetAadAsync(userId, connectionName, channelId, resourceUrls, agenticIdentity: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Get AAD tokens for specified resources.
    /// </summary>
    public async Task<IDictionary<string, GetTokenResult>?> GetAadAsync(string userId, string connectionName, string channelId, IList<string>? resourceUrls, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return await _client.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls?.ToArray(), CreateRequestContext(agenticIdentity), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the token status for a user's connections.
    /// </summary>
    public async Task<IList<GetTokenStatusResult>?> GetStatusAsync(string userId, string channelId, string? includeFilter = null, CancellationToken cancellationToken = default)
        => await GetStatusAsync(userId, channelId, includeFilter, agenticIdentity: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Get the token status for a user's connections.
    /// </summary>
    public async Task<IList<GetTokenStatusResult>?> GetStatusAsync(string userId, string channelId, string? includeFilter, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return await _client.GetTokenStatusAsync(userId, channelId, includeFilter, CreateRequestContext(agenticIdentity), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sign a user out of a connection.
    /// </summary>
    public Task SignOutAsync(string userId, string connectionName, string channelId, CancellationToken cancellationToken = default)
        => SignOutAsync(userId, connectionName, channelId, agenticIdentity: null, cancellationToken);

    /// <summary>
    /// Sign a user out of a connection.
    /// </summary>
    public Task SignOutAsync(string userId, string connectionName, string channelId, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return _client.SignOutUserAsync(userId, connectionName, channelId, CreateRequestContext(agenticIdentity), cancellationToken);
    }

    /// <summary>
    /// Exchange a token for another token.
    /// </summary>
    public async Task<GetTokenResult?> ExchangeAsync(string userId, string connectionName, string channelId, string exchangeToken, CancellationToken cancellationToken = default)
        => await ExchangeAsync(userId, connectionName, channelId, exchangeToken, agenticIdentity: null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Exchange a token for another token.
    /// </summary>
    public async Task<GetTokenResult?> ExchangeAsync(string userId, string connectionName, string channelId, string exchangeToken, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return await _client.ExchangeTokenAsync(userId, connectionName, channelId, exchangeToken, CreateRequestContext(agenticIdentity), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the sign-in URL for a connection.
    /// </summary>
    public Task<string?> GetSignInUrlAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
        => GetSignInUrlAsync(state, codeChallenge, emulatorUrl, finalRedirect, agenticIdentity: null, cancellationToken);

    /// <summary>
    /// Get the sign-in URL for a connection.
    /// </summary>
    public Task<string?> GetSignInUrlAsync(string state, string? codeChallenge, Uri? emulatorUrl, Uri? finalRedirect, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInUrlAsync(state, codeChallenge, emulatorUrl, finalRedirect, CreateRequestContext(agenticIdentity), cancellationToken);
    }

    /// <summary>
    /// Get the sign-in resource for a connection.
    /// </summary>
    public Task<GetSignInResourceResult> GetSignInResourceAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
        => GetSignInResourceAsync(state, codeChallenge, emulatorUrl, finalRedirect, agenticIdentity: null, cancellationToken);

    /// <summary>
    /// Get the sign-in resource for a connection.
    /// </summary>
    public Task<GetSignInResourceResult> GetSignInResourceAsync(string state, string? codeChallenge, Uri? emulatorUrl, Uri? finalRedirect, AgenticIdentity? agenticIdentity, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInResourceAsync(state, codeChallenge, emulatorUrl, finalRedirect, CreateRequestContext(agenticIdentity), cancellationToken);
    }

    private BotRequestContext? CreateRequestContext(AgenticIdentity? agenticIdentity) =>
        BotRequestContext.Merge(_requestContext, BotRequestContext.FromAgenticIdentity(agenticIdentity));
}
