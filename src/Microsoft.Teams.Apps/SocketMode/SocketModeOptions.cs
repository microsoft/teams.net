// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Configures Socket Mode, where the bot receives activities over outbound WebSocket connections instead of an
/// inbound HTTP endpoint.
/// </summary>
/// <remarks>
/// Socket Mode opens one connection per geo and waits until every geo is ready before the host finishes starting.
/// It is supported only in the public cloud, and runs on a host without a web server
/// (<c>Host.CreateApplicationBuilder</c>). Settings are validated when the host starts.
/// Socket Mode is experimental: the API is in preview and may change, and it is recommended only for developing agents.
/// </remarks>
[Experimental("ExperimentalTeamsSocketMode")]
public sealed class SocketModeOptions
{
    /// <summary>
    /// Gets or sets the base URL used to negotiate each geo connection. Must use HTTPS unless it targets loopback.
    /// Defaults to <c>https://botapi.skype.com</c>.
    /// </summary>
    public Uri NegotiateBaseUrl { get; set; } = new(SocketModeProtocol.DefaultNegotiateBaseUrl);

    /// <summary>
    /// Gets or sets the geos to connect, one connection each. An empty string connects to the base URL without a geo
    /// segment. Defaults to <c>amer</c>, <c>emea</c>, and <c>apac</c>.
    /// </summary>
    public IReadOnlyList<string> Geos { get; set; } = SocketModeProtocol.DefaultGeos;

    /// <summary>
    /// Snapshots the transport-level settings, so later changes to these options do not affect a running transport.
    /// Invalid values are passed through for the transport to reject when it starts.
    /// </summary>
    /// <returns>The transport options.</returns>
    internal SocketModeTransportOptions ToTransportOptions() => new()
    {
        NegotiateBaseUri = NegotiateBaseUrl,
        Geos = Geos is null ? null! : [.. Geos],
    };
}
