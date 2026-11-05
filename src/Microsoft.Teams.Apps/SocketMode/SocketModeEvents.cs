// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Provides observational, per-geo Socket Mode lifecycle events.
/// </summary>
/// <remarks>
/// Subscribe before starting the host to observe initial readiness. Handlers run synchronously and may run
/// concurrently for different geos; keep them short and thread-safe. Handler failures are logged and do not
/// interrupt delivery or recovery. Normal host shutdown does not raise <see cref="Disconnected"/>.
/// </remarks>
[Experimental("ExperimentalTeamsSocketMode")]
public sealed class SocketModeEvents
{
    private readonly ILogger _logger;

    internal SocketModeEvents(ILogger<SocketModeEvents> logger) => _logger = logger;

    /// <summary>
    /// Raised when a connection becomes ready, including replacements during recovery or planned token rotation.
    /// </summary>
    public event EventHandler<SocketModeReadyEventArgs>? Ready;

    /// <summary>
    /// Raised when delivery is interrupted, or recovery stops after a terminal negotiation failure.
    /// </summary>
    public event EventHandler<SocketModeDisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// Raised after readiness when delivery resumes following an unexpected interruption.
    /// Planned token rotation without an interruption does not raise this event.
    /// </summary>
    public event EventHandler<SocketModeReconnectedEventArgs>? Reconnected;

    internal void OnReady(string geo, SocketReadyFrame frame)
        => Notify(Ready, new SocketModeReadyEventArgs(geo, frame));

    internal void OnDisconnected(string geo, Exception? error, bool terminal)
        => Notify(Disconnected, new SocketModeDisconnectedEventArgs(geo, error, terminal));

    internal void OnReconnected(string geo)
        => Notify(Reconnected, new SocketModeReconnectedEventArgs(geo));

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Lifecycle observers must not interrupt delivery or prevent other observers from being notified.")]
    private void Notify<TEventArgs>(EventHandler<TEventArgs>? handlers, TEventArgs args)
        where TEventArgs : EventArgs
    {
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler<TEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Socket Mode lifecycle observer failed for {EventType}.", typeof(TEventArgs).Name);
            }
        }
    }
}

/// <summary>
/// Identifies a geo connection that is ready to receive activities.
/// </summary>
[Experimental("ExperimentalTeamsSocketMode")]
public sealed class SocketModeReadyEventArgs : EventArgs
{
    internal SocketModeReadyEventArgs(string geo, SocketReadyFrame frame)
    {
        Geo = geo;
        Frame = frame;
    }

    /// <summary>Gets the geo identifier.</summary>
    public string Geo { get; }

    /// <summary>Gets the service's ready frame, including the connection identifier when provided.</summary>
    public SocketReadyFrame Frame { get; }
}

/// <summary>
/// Describes an interruption to inbound delivery for a geo.
/// </summary>
[Experimental("ExperimentalTeamsSocketMode")]
public sealed class SocketModeDisconnectedEventArgs : EventArgs
{
    internal SocketModeDisconnectedEventArgs(string geo, Exception? error, bool terminal)
    {
        Geo = geo;
        Error = error;
        Terminal = terminal;
    }

    /// <summary>Gets the geo identifier.</summary>
    public string Geo { get; }

    /// <summary>Gets the failure that interrupted delivery or stopped recovery, when available.</summary>
    public Exception? Error { get; }

    /// <summary>
    /// Gets whether recovery has stopped after negotiation rejected the bot with HTTP 401 or 403.
    /// Correct the credentials or Socket Mode access and restart the host to recover.
    /// </summary>
    public bool Terminal { get; }
}

/// <summary>
/// Identifies a geo whose inbound delivery resumed after an unexpected interruption.
/// </summary>
[Experimental("ExperimentalTeamsSocketMode")]
public sealed class SocketModeReconnectedEventArgs : EventArgs
{
    internal SocketModeReconnectedEventArgs(string geo) => Geo = geo;

    /// <summary>Gets the geo identifier.</summary>
    public string Geo { get; }
}
