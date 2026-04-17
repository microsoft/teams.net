// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Http;

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

    internal UserClient(BotHttpClient http, string tokenApiEndpoint = "https://token.botframework.com")
    {
        Token = new UserTokenApiClient(http, tokenApiEndpoint);
    }
}
