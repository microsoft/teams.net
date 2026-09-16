// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// Addresses a shared drive item through Microsoft Graph's <c>/shares</c> endpoint.
/// </summary>
internal static class GraphShare
{
    /// <summary>Graph host root used when no cloud-specific one is configured.</summary>
    internal static readonly Uri DefaultBaseUrlRoot = new("https://graph.microsoft.com");

    /// <summary>
    /// Encode a sharing URL as a Microsoft Graph sharing token, for <c>GET /shares/{token}/driveItem/...</c>.
    /// <para>Graph's docs spell out base64, strip <c>=</c>, then swap <c>/</c> to <c>_</c> and <c>+</c> to <c>-</c>. Spelled out the same way here rather than through <c>System.Buffers.Text.Base64Url</c>, which exists only on net10.0 and later while this package also targets net8.0.</para>
    /// </summary>
    /// <param name="url">The sharing URL, passed through byte for byte: re-encoding or unescaping it produces a token that addresses a different item, or none.</param>
    internal static string EncodeSharingUrl(string url)
        => "u!" + Convert.ToBase64String(Encoding.UTF8.GetBytes(url)).TrimEnd('=').Replace('/', '_').Replace('+', '-');

    /// <summary>
    /// Build the Graph endpoint that streams a drive item's bytes, reached by its sharing URL.
    /// <para><paramref name="baseUrlRoot"/> is a host root such as <c>https://graph.microsoft.com</c>, matching what <c>BotFramework:GraphBaseUrl</c> supplies. The API version is appended here because a Graph client would append its own: a pre-versioned value produces <c>/v1.0/v1.0</c>, and a bare host 404s in a way that reads like a missing item.</para>
    /// </summary>
    /// <param name="sharingUrl">Browsable URL to the item, used as the sharing locator.</param>
    /// <param name="baseUrlRoot">Graph host root. Defaults to the public cloud. Must be https, or loopback for a local mock.</param>
    /// <exception cref="InvalidOperationException">The root is not https and is not loopback.</exception>
    internal static Uri BuildDriveItemContentUrl(Uri sharingUrl, Uri? baseUrlRoot = null)
    {
        ArgumentNullException.ThrowIfNull(sharingUrl);

        Uri resolved = baseUrlRoot ?? DefaultBaseUrlRoot;

        // The download URL is already required to be https and carries no bearer. This request does carry one, so it
        // gets at least the same check: a mistyped scheme would otherwise put a Graph token on the wire in cleartext.
        // Loopback over http stays allowed so a mock Graph in local development still works.
        if (!resolved.IsAbsoluteUri
            || (resolved.Scheme != Uri.UriSchemeHttps && !(resolved.Scheme == Uri.UriSchemeHttp && resolved.IsLoopback)))
        {
            throw new InvalidOperationException(
                $"cannot fetch file bytes through Graph: the Graph host root must use https, got '{resolved}'. This request carries a bearer token, so a cleartext root would put it on the wire.");
        }

        string root = resolved.ToString().TrimEnd('/');

        // `OriginalString` rather than `ToString()`: the latter unescapes percent-encoding, so a file whose name
        // contains a space would be encoded from a URL the service never issued.
        return new Uri($"{root}/v1.0/shares/{EncodeSharingUrl(sharingUrl.OriginalString)}/driveItem/content");
    }
}
