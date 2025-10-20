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
    /// URL used for client side combined authentication flow.
    /// </summary>
    public string? AccountLinkingUrl { get; set; }
}