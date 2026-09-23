// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Creates one-generation SignalR Socket Mode connections.
/// </summary>
internal sealed class SignalRSocketConnectionFactory : ISocketConnectionFactory
{
    private readonly ISocketModeNegotiator _negotiator;
    private readonly CreateSignalRClientConnection _createSignalRClientConnection;
    private readonly TimeSpan _readinessTimeout;
    private readonly TimeSpan _keepAliveInterval;
    private readonly TimeSpan _serverTimeout;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a SignalR Socket Mode connection factory.
    /// </summary>
    /// <param name="negotiator">Negotiates SignalR connection details.</param>
    /// <param name="readinessTimeout">The time allowed for the SocketReady frame.</param>
    /// <param name="keepAliveInterval">The interval between SignalR keep-alive messages.</param>
    /// <param name="serverTimeout">The interval before the SignalR server is considered unavailable.</param>
    /// <param name="logger">The logger for connection lifecycle failures.</param>
    /// <param name="createSignalRClientConnection">An optional SignalR client factory.</param>
    internal SignalRSocketConnectionFactory(
        ISocketModeNegotiator negotiator,
        TimeSpan readinessTimeout,
        TimeSpan keepAliveInterval,
        TimeSpan serverTimeout,
        ILogger logger,
        CreateSignalRClientConnection? createSignalRClientConnection = null)
    {
        _negotiator = negotiator ?? throw new ArgumentNullException(nameof(negotiator));
        _createSignalRClientConnection =
            createSignalRClientConnection ?? SignalRClientConnection.Create;
        _readinessTimeout = EnsurePositive(readinessTimeout, nameof(readinessTimeout));
        _keepAliveInterval = EnsurePositive(keepAliveInterval, nameof(keepAliveInterval));
        _serverTimeout = EnsurePositive(serverTimeout, nameof(serverTimeout));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
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
            _createSignalRClientConnection,
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

/// <summary>
/// Manages one generation of a SignalR Socket Mode connection.
/// </summary>
internal sealed class SignalRSocketConnection : ISocketConnection
{
    private readonly Uri _negotiateUri;
    private readonly SocketConnectionHandlers _handlers;
    private readonly ISocketModeNegotiator _negotiator;
    private readonly CreateSignalRClientConnection _createSignalRClientConnection;
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

    /// <summary>
    /// Initializes one generation of a SignalR Socket Mode connection.
    /// </summary>
    /// <param name="negotiateUri">The endpoint used to negotiate the connection.</param>
    /// <param name="handlers">Callbacks for frames and connection closure.</param>
    /// <param name="negotiator">Negotiates SignalR connection details.</param>
    /// <param name="createSignalRClientConnection">Creates the SignalR client connection.</param>
    /// <param name="readinessTimeout">The time allowed for the SocketReady frame.</param>
    /// <param name="keepAliveInterval">The interval between SignalR keep-alive messages.</param>
    /// <param name="serverTimeout">The interval before the SignalR server is considered unavailable.</param>
    /// <param name="logger">The logger for connection lifecycle failures.</param>
    internal SignalRSocketConnection(
        Uri negotiateUri,
        SocketConnectionHandlers handlers,
        ISocketModeNegotiator negotiator,
        CreateSignalRClientConnection createSignalRClientConnection,
        TimeSpan readinessTimeout,
        TimeSpan keepAliveInterval,
        TimeSpan serverTimeout,
        ILogger logger)
    {
        _negotiateUri = negotiateUri ?? throw new ArgumentNullException(nameof(negotiateUri));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _negotiator = negotiator ?? throw new ArgumentNullException(nameof(negotiator));
        _createSignalRClientConnection =
            createSignalRClientConnection
            ?? throw new ArgumentNullException(nameof(createSignalRClientConnection));
        _readinessTimeout = readinessTimeout;
        _keepAliveInterval = keepAliveInterval;
        _serverTimeout = serverTimeout;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public TimeSpan? TokenLifetime { get; private set; }

    /// <inheritdoc />
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

            ISignalRClientConnection connection = _createSignalRClientConnection(
                new Uri(negotiateResponse.Url!, UriKind.Absolute),
                negotiateResponse.AccessToken!,
                _keepAliveInterval,
                _serverTimeout);
            if (!TryPublishConnection(connection))
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw new OperationCanceledException(
                    "Socket Mode connection stopped before startup completed.",
                    startSource.Token);
            }

            connection.OnActivity(async envelope =>
            {
                await _readySource.Task
                    .WaitAsync(_lifetimeSource.Token)
                    .ConfigureAwait(false);
                return await _handlers.OnActivity(envelope).ConfigureAwait(false);
            });
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

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_stopLock)
        {
            return _stopTask ??= StopCoreAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
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
        bool planned = Volatile.Read(ref _stopped) != 0;

        if (!planned
            && Interlocked.CompareExchange(ref _readySettled, 1, 0) == 0)
        {
            _readySource.TrySetException(
                error ?? new IOException(
                    "Socket Mode connection closed before SocketReady."));
        }

        if (Interlocked.Exchange(ref _closedReported, 1) != 0)
        {
            return;
        }

        _handlers.OnClosed(error, planned);
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        await _lifetimeSource.CancelAsync().ConfigureAwait(false);

        ISignalRClientConnection? connection;
        lock (_stopLock)
        {
            connection = _connection;
        }

        if (connection is not null)
        {
            await connection
                .StopAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private bool TryPublishConnection(ISignalRClientConnection connection)
    {
        lock (_stopLock)
        {
            if (Volatile.Read(ref _stopped) != 0
                || Volatile.Read(ref _disposed) != 0)
            {
                return false;
            }

            _connection = connection;
            return true;
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
