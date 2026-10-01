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
    /// Gets or sets how long each geo has to establish its initial connection. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets an explicit reconnect delay schedule; the last delay repeats. When <c>null</c> or empty, capped
    /// exponential backoff with jitter is used.
    /// </summary>
    public IReadOnlyList<TimeSpan>? ReconnectDelays { get; set; }

    /// <summary>
    /// Gets or sets how long a new connection waits for the service to report it ready. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ReadinessTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval between keep-alive messages sent to the service. Defaults to 15 seconds.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets how long without a message from the service before the connection is considered lost.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Snapshots the transport-level settings, so later changes to these options do not affect a running transport.
    /// Invalid values are passed through for the transport to reject when it starts.
    /// </summary>
    /// <returns>The transport options.</returns>
    internal SocketModeTransportOptions ToTransportOptions() => new()
    {
        NegotiateBaseUri = NegotiateBaseUrl,
        Geos = Geos is null ? null! : [.. Geos],
        StartupTimeout = StartupTimeout,
        ReconnectDelays = ReconnectDelays is { Count: > 0 } delays ? [.. delays] : null,
    };
}
