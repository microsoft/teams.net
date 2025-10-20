// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// Settings for Deferred (User) auth flows
/// </summary>
public class OAuthSettings
{
    /// <summary>
    /// The default connection name to use
    /// </summary>
    public string DefaultConnectionName { get; set; } = "graph";

    /// <summary>
    /// Url used for client to perform tab auth and link the NAA account to the bot login account.
    /// </summary>
    public string? AccountLinkingUrl { get; set; }
}