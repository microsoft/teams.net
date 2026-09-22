// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.SocketMode;

internal sealed class SignalRSocketConnectionFactory : ISocketConnectionFactory
{
    private readonly ISocketModeNegotiator _negotiator;
    private readonly SignalRConnectionBuilder _createSignalRConnection;
    private readonly TimeSpan _readinessTimeout;
    private readonly TimeSpan _keepAliveInterval;
    private readonly TimeSpan _serverTimeout;
    private readonly ILogger _logger;

    internal SignalRSocketConnectionFactory(
        ISocketModeNegotiator negotiator,
        TimeSpan readinessTimeout,
        TimeSpan keepAliveInterval,
        TimeSpan serverTimeout,
        ILogger logger,
        SignalRConnectionBuilder? createSignalRConnection = null)
    {
        _negotiator = negotiator ?? throw new ArgumentNullException(nameof(negotiator));
        _createSignalRConnection =
            createSignalRConnection ?? SignalRClientConnection.Create;
        _readinessTimeout = EnsurePositive(readinessTimeout, nameof(readinessTimeout));
        _keepAliveInterval = EnsurePositive(keepAliveInterval, nameof(keepAliveInterval));
        _serverTimeout = EnsurePositive(serverTimeout, nameof(serverTimeout));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ISocketConnection Create(
        Uri negotiateUri,
        SocketConnectionHandlers handlers)
    {
        ArgumentNullException.ThrowIfNull(negotiateUri);
        ArgumentNullException.ThrowIfNull(handlers);

        return new SignalRSocketConnection(
            negotiateUri,
            handlers,
            _negotiator,
            _createSignalRConnection,
            _readinessTimeout,
            _keepAliveInterval,
            _serverTimeout,
            _logger);
    }

    private static TimeSpan EnsurePositive(TimeSpan value, string parameterName)
        => value > TimeSpan.Zero
            ? value
            : throw new ArgumentOutOfRangeException(
                parameterName,
                "Socket Mode connection timeouts and intervals must be greater than zero.");
}

internal sealed class SignalRSocketConnection : ISocketConnection
{
    private readonly Uri _negotiateUri;
    private readonly SocketConnectionHandlers _handlers;
    private readonly ISocketModeNegotiator _negotiator;
    private readonly SignalRConnectionBuilder _createSignalRConnection;
    private readonly TimeSpan _readinessTimeout;
    private readonly TimeSpan _keepAliveInterval;
    private readonly TimeSpan _serverTimeout;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _lifetimeSource = new();
    private readonly TaskCompletionSource _readySource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _stopLock = new();

    private ISignalRClientConnection? _connection;
    private Task? _stopTask;
    private int _started;
    private int _stopped;
    private int _disposed;
    private int _readySettled;
    private int _closedReported;

    internal SignalRSocketConnection(
        Uri negotiateUri,
        SocketConnectionHandlers handlers,
        ISocketModeNegotiator negotiator,
        SignalRConnectionBuilder createSignalRConnection,
        TimeSpan readinessTimeout,
        TimeSpan keepAliveInterval,
        TimeSpan serverTimeout,
        ILogger logger)
    {
        _negotiateUri = negotiateUri ?? throw new ArgumentNullException(nameof(negotiateUri));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _negotiator = negotiator ?? throw new ArgumentNullException(nameof(negotiator));
        _createSignalRConnection =
            createSignalRConnection
            ?? throw new ArgumentNullException(nameof(createSignalRConnection));
        _readinessTimeout = readinessTimeout;
        _keepAliveInterval = keepAliveInterval;
        _serverTimeout = serverTimeout;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public TimeSpan? TokenLifetime { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException(
                "A Socket Mode connection generation can only be started once.");
        }

        using CancellationTokenSource startSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeSource.Token);

        try
        {
            SocketModeNegotiateResponse negotiateResponse =
                await _negotiator
                    .NegotiateAsync(_negotiateUri, startSource.Token)
                    .ConfigureAwait(false);

            TokenLifetime = negotiateResponse.ExpiresIn > 0
                ? TimeSpan.FromSeconds(negotiateResponse.ExpiresIn)
                : null;

            ISignalRClientConnection connection = _createSignalRConnection(
                new Uri(negotiateResponse.Url!, UriKind.Absolute),
                negotiateResponse.AccessToken!,
                _keepAliveInterval,
                _serverTimeout);
            _connection = connection;

            connection.OnActivity(_handlers.OnActivity);
            connection.OnReady(HandleReady);
            connection.OnClosed(HandleClosed);

            await connection
                .StartAsync(startSource.Token)
                .ConfigureAwait(false);

            try
            {
                await _readySource.Task
                    .WaitAsync(_readinessTimeout, startSource.Token)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException(
                    $"Socket Mode readiness timed out after {_readinessTimeout}.",
                    exception);
            }
        }
        catch
        {
            await StopAfterFailedStartAsync().ConfigureAwait(false);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_stopLock)
        {
            return _stopTask ??= StopCoreAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            ISignalRClientConnection? connection = _connection;
            _connection = null;
            if (connection is not null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            _lifetimeSource.Dispose();
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The readiness gate must remain settled even when an observer throws; the failure is logged.")]
    private void HandleReady(SocketReadyFrame frame)
    {
        if (_lifetimeSource.IsCancellationRequested
            || Interlocked.CompareExchange(ref _readySettled, 1, 0) != 0)
        {
            return;
        }

        _readySource.TrySetResult();

        try
        {
            _handlers.OnReady(frame);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Socket Mode ready observer failed after the connection became ready.");
        }
    }

    private void HandleClosed(Exception? error)
    {
        if (Interlocked.CompareExchange(ref _readySettled, 1, 0) == 0)
        {
            _readySource.TrySetException(
                error ?? new IOException(
                    "Socket Mode connection closed before SocketReady."));
        }

        if (Interlocked.Exchange(ref _closedReported, 1) != 0)
        {
            return;
        }

        _handlers.OnClosed(error);
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        await _lifetimeSource.CancelAsync().ConfigureAwait(false);

        ISignalRClientConnection? connection = _connection;
        if (connection is not null)
        {
            await connection
                .StopAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Cleanup failure is logged so the original startup exception remains the reported failure.")]
    private async Task StopAfterFailedStartAsync()
    {
        try
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Socket Mode connection cleanup failed after startup failure.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposed) != 0,
            this);
    }
}
