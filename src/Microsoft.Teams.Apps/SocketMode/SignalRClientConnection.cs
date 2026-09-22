// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.Teams.Apps.SocketMode;

internal delegate ISignalRClientConnection SignalRConnectionBuilder(
    Uri url,
    string accessToken,
    TimeSpan keepAliveInterval,
    TimeSpan serverTimeout);

internal interface ISignalRClientConnection : IAsyncDisposable
{
    void OnActivity(Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> handler);

    void OnReady(Action<SocketReadyFrame> handler);

    void OnClosed(Action<Exception?> handler);

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

internal sealed class SignalRClientConnection(HubConnection connection)
    : ISignalRClientConnection
{
    private readonly HubConnection _connection =
        connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly List<IDisposable> _subscriptions = [];
    private int _disposed;

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

    public void OnReady(Action<SocketReadyFrame> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ThrowIfDisposed();

        _subscriptions.Add(
            _connection.On<SocketReadyFrame>(
                "SocketReady",
                handler));
    }

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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _connection.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return Task.CompletedTask;
        }

        return _connection.StopAsync(cancellationToken);
    }

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
