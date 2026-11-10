// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Provides Socket Mode lifecycle events and read-only connection diagnostics.
/// The host owns starting and stopping the transport.
/// </summary>
[Experimental("ExperimentalTeamsSocketMode")]
public sealed class SocketModeObserver
{
    private readonly Lazy<SocketModeTransport> _transport;

    internal SocketModeObserver(Func<SocketModeTransport> getTransport, SocketModeEvents events)
    {
        // Defer transport creation so subscribing does not start validation or resolve the app being constructed.
        _transport = new Lazy<SocketModeTransport>(getTransport);
        Events = events;
    }

    /// <summary>Gets the per-geo lifecycle events.</summary>
    public SocketModeEvents Events { get; }

    /// <summary>
    /// Gets the aggregate status: ready only when every geo is ready, and disconnected when any geo is recovering
    /// or has stopped after a terminal negotiation failure.
    /// </summary>
    public SocketModeStatus Status => _transport.Value.Status;

    /// <summary>Gets a snapshot of each geo's status.</summary>
    public IReadOnlyDictionary<string, SocketModeStatus> GeoStatuses => _transport.Value.GeoStatuses;

    /// <summary>Gets the resolved geo identifiers the transport connects to.</summary>
    public IReadOnlyList<string> GeoList => _transport.Value.GeoList;

    /// <summary>
    /// Gets the resolved negotiation endpoint for the first configured geo, for diagnostics.
    /// Each geo connects to its own endpoint.
    /// </summary>
    /// <remarks>This URL is managed by the SDK and is for diagnostics only. Do not depend on it; it may change in the future.</remarks>
    public Uri NegotiateUrl => _transport.Value.NegotiateUrl;
}
