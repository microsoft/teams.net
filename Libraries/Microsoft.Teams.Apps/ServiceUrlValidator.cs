// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Validates service URLs against known allowed domains.
/// </summary>
public static class ServiceUrlValidator
{
    /// <summary>
    /// Validates that a service URL hostname is allowed.
    /// Checks against the cloud environment's allowed service URLs,
    /// plus any additional domains provided by the caller.
    /// Localhost is always allowed for local development.
    /// </summary>
    public static bool IsAllowed(string serviceUrl, CloudEnvironment cloud, IEnumerable<string>? additionalDomains = null)
    {
        if (string.IsNullOrEmpty(serviceUrl))
            return true; // No URL to validate

        if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
            return false;

        var hostname = uri.Host.ToLowerInvariant();

        if (hostname is "localhost" or "127.0.0.1")
            return true;

        var additional = additionalDomains?.ToList() ?? [];
        if (additional.Contains("*"))
            return true;

        // Check against cloud environment's allowed FQDNs
        if (cloud.AllowedServiceUrls.Any(allowed => hostname == allowed.ToLowerInvariant()))
            return true;

        // Check against additional domains (suffix match)
        return additional.Any(domain => hostname.EndsWith(domain.ToLowerInvariant()));
    }
}
