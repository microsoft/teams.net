// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper aggregating bot token and sign-in operations.
/// </summary>
public class BotClient
{
    internal BotClient(CoreUserTokenClient client)
    {
#pragma warning disable CS0618 // Obsolete
        Token = new BotTokenClient();
#pragma warning restore CS0618
        SignIn = new BotSignInClient(client);
    }

#pragma warning disable CS0618 // Obsolete
    public BotTokenClient Token { get; }
#pragma warning restore CS0618

    public BotSignInClient SignIn { get; }
}
