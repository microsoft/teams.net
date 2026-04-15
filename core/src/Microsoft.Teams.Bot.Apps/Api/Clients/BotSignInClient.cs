// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper for bot sign-in operations.
/// Delegates to <see cref="CoreUserTokenClient"/>.
/// </summary>
public class BotSignInClient
{
    private readonly CoreUserTokenClient _client;

    internal BotSignInClient(CoreUserTokenClient client)
    {
        _client = client;
    }

    public Task<GetSignInResourceResult> GetResourceAsync(string userId, string connectionName, string channelId, string? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInResource(userId, connectionName, channelId, finalRedirect, cancellationToken);
    }

    [Obsolete("GetUrlAsync is not supported in the new SDK. Use GetResourceAsync() and access .SignInLink instead.")]
    public Task<string> GetUrlAsync()
    {
        throw new NotSupportedException("GetUrlAsync is not supported in the new SDK. Use GetResourceAsync() and access .SignInLink instead.");
    }
}
