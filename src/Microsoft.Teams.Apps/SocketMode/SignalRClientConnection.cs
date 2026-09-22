// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Creates a SignalR client connection for a negotiated Socket Mode endpoint.
/// </summary>
/// <param name="url">The negotiated SignalR endpoint.</param>
/// <param name="accessToken">The negotiated access token.</param>
/// <param name="keepAliveInterval">The interval between SignalR keep-alive messages.</param>
/// <param name="serverTimeout">The interval before the SignalR server is considered unavailable.</param>
/// <returns>A SignalR client connection.</returns>
internal delegate ISignalRClientConnection SignalRConnectionBuilder(
    Uri url,
    string accessToken,
    TimeSpan keepAliveInterval,
    TimeSpan serverTimeout);

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
    private int _disposed;

    /// <summary>
    /// Creates a SignalR client for a negotiated Socket Mode endpoint.
    /// </summary>
    /// <param name="url">The negotiated SignalR endpoint.</param>
    /// <param name="accessToken">The negotiated access token.</param>
    /// <param name="keepAliveInterval">The interval between keep-alive messages.</param>
    /// <param name="serverTimeout">The interval before the server is considered unavailable.</param>
    /// <returns>A configured SignalR client connection.</returns>
    internal static ISignalRClientConnection Create(
        Uri url,
        string accessToken,
        TimeSpan keepAliveInterval,
        TimeSpan serverTimeout)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider =
                    () => Task.FromResult<string?>(accessToken);
            })
            .WithKeepAliveInterval(keepAliveInterval)
            .WithServerTimeout(serverTimeout)
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

        _connection.Closed += exception =>
        {
            handler(exception);
            return Task.CompletedTask;
        };
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
