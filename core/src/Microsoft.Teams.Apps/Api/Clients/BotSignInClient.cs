// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for bot sign-in operations.
/// Delegates to the core <see cref="CoreUserTokenClient"/>.
/// </summary>
public class BotSignInClient
{
    private readonly CoreUserTokenClient _client;

    internal BotSignInClient(CoreUserTokenClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Get the sign-in URL for a connection.
    /// </summary>
    public Task<string?> GetUrlAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInUrlAsync(state, codeChallenge, emulatorUrl, finalRedirect, cancellationToken);
    }

    /// <summary>
    /// Get the sign-in resource for a connection.
    /// </summary>
    public Task<GetSignInResourceResult?> GetResourceAsync(string state, string? codeChallenge = null, Uri? emulatorUrl = null, Uri? finalRedirect = null, CancellationToken cancellationToken = default)
    {
        return _client.GetSignInResourceAsync(state, codeChallenge, emulatorUrl, finalRedirect, cancellationToken)!;
    }
}
