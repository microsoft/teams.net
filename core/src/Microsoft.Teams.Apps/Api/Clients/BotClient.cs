// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Client for bot-level operations, including the sign-in sub-client.
/// </summary>
public class BotClient
{
    /// <summary>
    /// Client for bot sign-in operations.
    /// </summary>
    public BotSignInClient SignIn { get; }

    internal BotClient(CoreUserTokenClient userTokenClient)
    {
        SignIn = new BotSignInClient(userTokenClient);
    }
}
