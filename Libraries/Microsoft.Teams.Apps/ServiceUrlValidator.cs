// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// Validates service URLs against known allowed domains.
/// </summary>
public static class ServiceUrlValidator
{
    /// <summary>
    /// Default allowed service URL domain suffixes for Bot Framework.
    /// Covers public, government, sovereign, and regional clouds.
    /// </summary>
    private static readonly string[] DefaultAllowedDomains =
    [
        // Public cloud
        ".botframework.com",
        // US Government
        ".botframework.azure.us",
        ".teams.microsoft.com",
        ".teams.microsoft.us",
        // China (21Vianet)
        ".botframework.azure.cn",
        ".teams.microsoftonline.cn",
    ];

    /// <summary>
    /// Validates that a service URL belongs to a known allowed domain.
    /// Returns true if the URL's hostname ends with one of the allowed domain suffixes,
    /// or if the hostname is localhost (for local development).
    /// </summary>
    public static bool IsAllowed(string serviceUrl, IEnumerable<string>? additionalDomains = null)
    {
        if (string.IsNullOrEmpty(serviceUrl))
            return true; // No URL to validate

        if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
            return false;

        var hostname = uri.Host.ToLowerInvariant();

        if (hostname is "localhost" or "127.0.0.1")
            return true;

        // trafficmanager.net is a shared Azure service; only allow smba-prefixed hostnames
        if (hostname.EndsWith(".trafficmanager.net") || hostname == "trafficmanager.net")
            return hostname.StartsWith("smba");

        var allDomains = additionalDomains is not null
            ? DefaultAllowedDomains.Concat(additionalDomains)
            : DefaultAllowedDomains;

        return allDomains.Any(domain => hostname.EndsWith(domain.ToLowerInvariant()));
    }
}
