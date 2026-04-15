// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CoreUserTokenClient = Microsoft.Teams.Bot.Core.UserTokenClient;

#pragma warning disable CS1591
namespace Microsoft.Teams.Bot.Apps.Api.Clients;

/// <summary>
/// Backward-compatible wrapper aggregating user token operations.
/// </summary>
public class UserClient
{
    internal UserClient(CoreUserTokenClient client)
    {
        Token = new UserTokenClient(client);
    }

    public UserTokenClient Token { get; }
}
