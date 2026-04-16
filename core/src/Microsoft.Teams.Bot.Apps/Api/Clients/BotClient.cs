// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Http;

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

    internal BotClient(BotHttpClient http, string tokenApiEndpoint = "https://token.botframework.com")
    {
        SignIn = new BotSignInClient(http, tokenApiEndpoint);
    }
}
