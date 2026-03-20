// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps.Api;

/// <summary>
/// Provides user-related operations.
/// </summary>
/// <remarks>
/// This class serves as a container for user-specific sub-APIs:
/// <list type="bullet">
/// <item><see cref="Token"/> - User token operations (OAuth SSO)</item>
/// </list>
/// </remarks>
public class UsersApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsersApi"/> class.
    /// </summary>
    /// <param name="userTokenClient">The user token client for token operations.</param>
    internal UsersApi(UserTokenClient userTokenClient)
    {
        Token = new UserTokenApi(userTokenClient);
    }

    /// <summary>
    /// Gets the token API for user token operations (OAuth SSO).
    /// </summary>
    public UserTokenApi Token { get; }
}
