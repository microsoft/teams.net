// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Creates a SignalR client connection for a negotiated Socket Mode endpoint.
/// </summary>
/// <param name="url">The negotiated SignalR endpoint.</param>
/// <param name="accessToken">The negotiated access token.</param>
/// <returns>A SignalR client connection.</returns>
internal delegate ISignalRClientConnection CreateSignalRClientConnection(
    Uri url,
    string accessToken);

/// <summary>
/// Provides the SignalR operations required by a Socket Mode connection.
/// </summary>
internal interface ISignalRClientConnection : IAsyncDisposable
{
    /// <summary>
    /// Registers the handler for activity envelopes.
    /// </summary>
    /// <param name="handler">The activity handler that returns an optional reply frame.</param>
    void OnActivity(Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> handler);

    /// <summary>
    /// Registers the handler for the SocketReady frame.
    /// </summary>
    /// <param name="handler">The ready frame handler.</param>
    void OnReady(Action<SocketReadyFrame> handler);

    /// <summary>
    /// Registers the handler for terminal connection closure.
    /// </summary>
    /// <param name="handler">The closure handler.</param>
    void OnClosed(Action<Exception?> handler);

    /// <summary>
    /// Starts the SignalR connection.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the SignalR connection.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task StopAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Adapts the official SignalR client to the Socket Mode transport.
/// </summary>
/// <param name="connection">The underlying SignalR hub connection.</param>
internal sealed class SignalRClientConnection(HubConnection connection)
    : ISignalRClientConnection
{
    private readonly HubConnection _connection =
        connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<Func<Exception?, Task>> _closedHandlers = [];
    private int _disposed;

    /// <summary>
    /// Creates a SignalR client for a negotiated Socket Mode endpoint.
    /// </summary>
    /// <param name="url">The negotiated SignalR endpoint.</param>
    /// <param name="accessToken">The negotiated access token.</param>
    /// <returns>A configured SignalR client connection.</returns>
    internal static ISignalRClientConnection Create(
        Uri url,
        string accessToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider =
                    () => Task.FromResult<string?>(accessToken);

                // Each negotiated token is valid for exactly one socket, so never fall back to another transport.
                options.Transports = HttpTransportType.WebSockets;
            })
            .Build();

        return new SignalRClientConnection(connection);
    }

    /// <inheritdoc />
    public void OnActivity(
        Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        _subscriptions.Add(
            _connection.On<SocketActivityEnvelope, SocketReplyFrame?>(
                "Activity",
                handler));
    }

    /// <inheritdoc />
    public void OnReady(Action<SocketReadyFrame> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        _subscriptions.Add(
            _connection.On<SocketReadyFrame>(
                "SocketReady",
                handler));
    }

    /// <inheritdoc />
    public void OnClosed(Action<Exception?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        Func<Exception?, Task> closedHandler = exception =>
        {
            handler(exception);
            return Task.CompletedTask;
        };
        _connection.Closed += closedHandler;
        _closedHandlers.Add(closedHandler);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _connection.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return Task.CompletedTask;
        }

        return _connection.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (Func<Exception?, Task> closedHandler in _closedHandlers)
        {
            _connection.Closed -= closedHandler;
        }

        _closedHandlers.Clear();

        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposed) != 0,
            this);
    }
}
