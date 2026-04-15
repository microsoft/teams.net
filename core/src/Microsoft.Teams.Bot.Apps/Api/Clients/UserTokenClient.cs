// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for user token operations.
/// Delegates to <see cref="CoreUserTokenClient"/>.
/// </summary>
public class UserTokenClient
{
    private readonly CoreUserTokenClient _client;

    internal UserTokenClient(CoreUserTokenClient client)
    {
        _client = client;
    }

    public Task<GetTokenResult?> GetAsync(string userId, string connectionName, string channelId, string? code = null, CancellationToken cancellationToken = default)
    {
        return _client.GetTokenAsync(userId, connectionName, channelId, code, cancellationToken);
    }

    public Task<IDictionary<string, GetTokenResult>> GetAadAsync(string userId, string connectionName, string channelId, string[]? resourceUrls = null, CancellationToken cancellationToken = default)
    {
        return _client.GetAadTokensAsync(userId, connectionName, channelId, resourceUrls, cancellationToken);
    }

    public Task<GetTokenStatusResult[]> GetStatusAsync(string userId, string channelId, string? include = null, CancellationToken cancellationToken = default)
    {
        return _client.GetTokenStatusAsync(userId, channelId, include, cancellationToken);
    }

    public Task SignOutAsync(string userId, string? connectionName = null, string? channelId = null, CancellationToken cancellationToken = default)
    {
        return _client.SignOutUserAsync(userId, connectionName, channelId, cancellationToken);
    }

    public Task<GetTokenResult> ExchangeAsync(string userId, string connectionName, string channelId, string? exchangeToken, CancellationToken cancellationToken = default)
    {
        return _client.ExchangeTokenAsync(userId, connectionName, channelId, exchangeToken, cancellationToken);
    }
}
