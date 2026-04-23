// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for user-level operations, including the token sub-client.
/// </summary>
public class UserClient
{
    /// <summary>
    /// Client for user token operations.
    /// </summary>
    public UserTokenApiClient Token { get; }

    internal UserClient(CoreUserTokenClient userTokenClient)
    {
        Token = new UserTokenApiClient(userTokenClient);
    }
}
