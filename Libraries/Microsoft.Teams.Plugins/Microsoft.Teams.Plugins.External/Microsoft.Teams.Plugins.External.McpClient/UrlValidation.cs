// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Sockets;

namespace Microsoft.Teams.Plugins.External.McpClient;

public class UrlValidationException : Exception
{
    public UrlValidationException(string message) : base(message) { }
    public UrlValidationException(string message, Exception inner) : base(message, inner) { }
}

public static class UrlValidation
{
    /// <summary>
    /// Test seam: override to mock DNS lookups. Defaults to <see cref="Dns.GetHostAddressesAsync(string, CancellationToken)"/>.
    /// </summary>
    internal static Func<string, CancellationToken, Task<IPAddress[]>> HostResolver { get; set; } =
        (host, ct) => Dns.GetHostAddressesAsync(host, ct);

    /// <summary>
    /// Validates a URL destined for an MCP server connection. When
    /// <paramref name="validateUrl"/> is provided, it fully replaces the default
    /// checks. Otherwise the default policy rejects non-http(s) schemes, and
    /// (unless <paramref name="allowPrivateNetwork"/> is <c>true</c>) rejects
    /// URLs whose hostname resolves to a private / loopback / link-local address.
    /// </summary>
    /// <exception cref="UrlValidationException">Thrown on rejection.</exception>
    public static async Task<Uri> ValidateMcpServerUrlAsync(
        Uri url,
        bool allowPrivateNetwork = false,
        Func<Uri, Task<bool>>? validateUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (validateUrl is not null)
        {
            bool allowed = await validateUrl(url);
            if (!allowed)
            {
                throw new UrlValidationException($"URL rejected by ValidateUrl: {url}");
            }
            return url;
        }

        if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
        {
            throw new UrlValidationException(
                $"URL scheme {url.Scheme} is not allowed; must be http or https"
            );
        }

        if (allowPrivateNetwork)
        {
            return url;
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(url.Host, out var literal))
        {
            addresses = new[] { literal };
        }
        else
        {
            try
            {
                addresses = await HostResolver(url.Host, cancellationToken);
            }
            catch (SocketException ex)
            {
                throw new UrlValidationException(
                    $"Could not resolve host {url.Host}: {ex.Message}", ex
                );
            }
        }

        foreach (var address in addresses)
        {
            if (IsPrivateAddress(address))
            {
                throw new UrlValidationException(
                    $"URL {url} resolves to private or loopback address {address}; " +
                    "set AllowPrivateNetwork to true to bypass"
                );
            }
        }

        return url;
    }

    /// <summary>
    /// True if the address is loopback, RFC1918 private, link-local, or an
    /// IPv6 unique-local / link-local address.
    /// </summary>
    public static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal) return true;
            if (address.IsIPv6SiteLocal) return true;
            if (IsIPv6UniqueLocal(address)) return true;
            if (address.IsIPv4MappedToIPv6)
            {
                return IsPrivateIpv4(address.MapToIPv4());
            }
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsPrivateIpv4(address);
        }

        // Unknown address family: fail closed.
        return true;
    }

    private static bool IsPrivateIpv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4) return false;

        // 10.0.0.0/8
        if (bytes[0] == 10) return true;
        // 172.16.0.0/12
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        // 192.168.0.0/16
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        // 169.254.0.0/16 link-local
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        return false;
    }

    private static bool IsIPv6UniqueLocal(IPAddress address)
    {
        // fc00::/7 -> first byte is 0xfc or 0xfd
        var bytes = address.GetAddressBytes();
        return bytes.Length == 16 && (bytes[0] == 0xfc || bytes[0] == 0xfd);
    }
}