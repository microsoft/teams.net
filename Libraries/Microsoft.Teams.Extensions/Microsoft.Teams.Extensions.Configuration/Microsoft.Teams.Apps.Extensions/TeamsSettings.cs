// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsSettings
{
    /// <summary>
    /// The ID assigned to your application.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The secret (ie password) for your application.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The Tenant ID assigned to your application (for single tenant apps only)
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// URL used for client side combined authentication flow.
    /// </summary>
    public string? AccountLinkingUrl { get; set; }

    /// <summary>
    /// true when <code>ClientId</code> OR <code>ClientSecret</code> are empty
    /// </summary>
    public bool Empty => string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret);

    /// <summary>
    /// Apply settings to app options.
    /// </summary>
    public AppOptions Apply(AppOptions? options = null)
    {
        options ??= new AppOptions();

        if (ClientId is not null && ClientSecret is not null && !Empty)
        {
            options.Credentials = new ClientCredentials(ClientId, ClientSecret, TenantId);
        }

        if (AccountLinkingUrl is null)
        {
            options.OAuth.AccountLinkingUrl = AccountLinkingUrl;
        }

        return options;
    }
}