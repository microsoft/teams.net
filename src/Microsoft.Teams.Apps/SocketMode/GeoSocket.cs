// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Provides the retry policy, dispatch, and lifecycle reporting a <see cref="GeoSocket"/> needs from its transport.
/// </summary>
internal interface IGeoSocketOwner
{
    /// <summary>
    /// Gets the time allowed for the initial connection to become ready. Zero permits a single unbounded attempt.
    /// </summary>
    TimeSpan StartupTimeout { get; }

    /// <summary>
    /// Gets how long before token expiry a replacement connection is established.
    /// </summary>
    TimeSpan TokenRefreshMargin { get; }

    /// <summary>
    /// Gets how long a replaced connection keeps dispatching after its successor is ready.
    /// </summary>
    TimeSpan HandoffWindow { get; }

    /// <summary>
    /// Gets the server-requested retry delay for a failure, when one was provided.
    /// </summary>
    /// <param name="error">The failure that caused the retry.</param>
    TimeSpan? GetRetryAfter(Exception? error);

    /// <summary>
    /// Gets the backoff delay for a zero-based retry attempt.
    /// </summary>
    /// <param name="attempt">The zero-based retry attempt.</param>
    TimeSpan GetBackoffDelay(int attempt);

    /// <summary>
    /// Dispatches an activity received on a live connection for a geo.
    /// </summary>
    /// <param name="geo">The geo that received the activity.</param>
    /// <param name="envelope">The received activity envelope.</param>
    Task<SocketReplyFrame?> DispatchAsync(string geo, SocketActivityEnvelope envelope);

    /// <summary>
    /// Reports that a connection generation for a geo received SocketReady.
    /// </summary>
    /// <param name="geo">The geo whose connection became ready.</param>
    /// <param name="frame">The ready frame.</param>
    void OnGeoReady(string geo, SocketReadyFrame frame);

    /// <summary>
    /// Reports that inbound delivery for a geo was unexpectedly interrupted.
    /// </summary>
    /// <param name="geo">The geo that disconnected.</param>
    /// <param name="error">The failure that closed the connection, when available.</param>
    void OnGeoDisconnected(string geo, Exception? error);

    /// <summary>
    /// Reports that inbound delivery for a geo resumed after an unexpected interruption.
    /// </summary>
    /// <param name="geo">The geo that reconnected.</param>
    void OnGeoReconnected(string geo);
}

/// <summary>
/// Keeps one geo connected: establishes the initial connection within a startup budget, reconnects after
/// unexpected closure, and rotates tokens make-before-break.
/// </summary>
/// <remarks>
/// Each connection attempt is a new generation. Activities are dispatched only from the active ready generation
/// or a predecessor that is explicitly retiring during a token rotation handoff.
/// </remarks>
internal sealed class GeoSocket : IAsyncDisposable
{
    private static readonly TimeSpan MinimumRefreshDelay = TimeSpan.FromSeconds(1);

    private readonly IGeoSocketOwner _owner;
    private readonly Uri _negotiateUri;
    private readonly ISocketConnectionFactory _connectionFactory;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _stopSource = new();
    private readonly CancellationToken _stopToken;
    private readonly object _sync = new();
    private readonly HashSet<ISocketConnection> _owned = [];
    private readonly HashSet<long> _retiring = [];
    private readonly HashSet<Task> _retirements = [];

    private long _generation;
    private Generation? _active;
    private ITimer? _refreshTimer;
    private Task? _supervisor;
    private Task? _stopTask;
    private bool _disconnected;
    private bool _stopping;
    private int _started;
    private int _disposed;

    /// <summary>
    /// Initializes a supervisor for one geo.
    /// </summary>
    /// <param name="owner">The transport that owns this geo.</param>
    /// <param name="geo">The geo identifier. Empty when the negotiate URL has no geo segment.</param>
    /// <param name="negotiateUri">The negotiate endpoint for the geo.</param>
    /// <param name="connectionFactory">Creates one connection per generation.</param>
    /// <param name="logger">The logger for lifecycle events.</param>
    /// <param name="timeProvider">The time provider for startup budgets, retries, rotation, and handoff.</param>
    internal GeoSocket(
        IGeoSocketOwner owner,
        string geo,
        Uri negotiateUri,
        ISocketConnectionFactory connectionFactory,
        ILogger logger,
        TimeProvider? timeProvider = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Geo = geo ?? throw new ArgumentNullException(nameof(geo));
        _negotiateUri = negotiateUri ?? throw new ArgumentNullException(nameof(negotiateUri));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _stopToken = _stopSource.Token;
    }

    /// <summary>
    /// Gets the geo identifier.
    /// </summary>
    internal string Geo { get; }

    /// <summary>
    /// Establishes the initial ready connection, then supervises it in the background.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling startup.</param>
    /// <exception cref="TimeoutException">Thrown when no generation becomes ready within the startup budget.</exception>
    internal async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("A GeoSocket can only be started once.");
        }

        Generation initial = await ConnectInitialAsync(cancellationToken).ConfigureAwait(false);

        lock (_sync)
        {
            if (!_stopping)
            {
                _supervisor = SuperviseAsync(initial);
                return;
            }
        }

        throw new OperationCanceledException("Socket Mode geo stopped before startup completed.");
    }

    /// <summary>
    /// Stops supervision and stops and disposes every connection this geo owns.
    /// </summary>
    internal Task StopAsync()
    {
        lock (_sync)
        {
            _stopping = true;
            return _stopTask ??= StopCoreAsync();
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
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _stopSource.Dispose();
        }
    }

    private async Task<Generation> ConnectInitialAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource startSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopToken);
        TimeSpan budget = _owner.StartupTimeout;
        long startedAt = _timeProvider.GetTimestamp();

        for (int attempt = 0; ; attempt++)
        {
            using CancellationTokenSource deadlineSource = new(Timeout.InfiniteTimeSpan, _timeProvider);
            if (budget > TimeSpan.Zero)
            {
                deadlineSource.CancelAfter(RemainingBudget(budget, startedAt));
            }

            using CancellationTokenSource attemptSource =
                CancellationTokenSource.CreateLinkedTokenSource(startSource.Token, deadlineSource.Token);

            try
            {
                return await ConnectAsync(attemptSource.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (!startSource.IsCancellationRequested)
            {
                Exception error = deadlineSource.IsCancellationRequested
                    ? new TimeoutException(
                        $"Socket Mode geo '{Geo}' did not become ready within {budget}.",
                        exception)
                    : exception;
                TimeSpan delay = GetRetryDelay(error, attempt);

                if (budget <= TimeSpan.Zero || delay >= RemainingBudget(budget, startedAt))
                {
                    if (ReferenceEquals(error, exception))
                    {
                        throw;
                    }

                    throw error;
                }

                _logger.LogWarning(
                    exception,
                    "Socket Mode geo {Geo} initial connection failed; retrying in {Delay}.",
                    Geo,
                    delay);
                await Task.Delay(delay, _timeProvider, startSource.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task SuperviseAsync(Generation current)
    {
        try
        {
            while (true)
            {
                CloseReason reason = await current.Closed.Task.WaitAsync(_stopToken).ConfigureAwait(false);

                if (reason.Planned)
                {
                    _logger.LogInformation("Socket Mode geo {Geo} rotating connection before token expiry.", Geo);
                    Generation replacement = await ReconnectAsync(null, delayFirstAttempt: false).ConfigureAwait(false);
                    StartRetirement(current);
                    current = replacement;
                }
                else
                {
                    await ReleaseAsync(current.Connection).ConfigureAwait(false);
                    current = await ReconnectAsync(reason.Error, delayFirstAttempt: true).ConfigureAwait(false);
                }

                ReportReconnected();
            }
        }
        catch (OperationCanceledException) when (_stopToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Socket Mode geo {Geo} supervisor stopped unexpectedly.", Geo);
            throw;
        }
    }

    private async Task<Generation> ReconnectAsync(Exception? previousError, bool delayFirstAttempt)
    {
        Exception? error = previousError;
        int retry = 0;

        for (int attempt = 1; ; attempt++)
        {
            if (delayFirstAttempt || attempt > 1)
            {
                await Task.Delay(GetRetryDelay(error, retry++), _timeProvider, _stopToken).ConfigureAwait(false);
            }

            try
            {
                return await ConnectAsync(_stopToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!_stopToken.IsCancellationRequested)
            {
                error = exception;
                _logger.LogWarning(
                    exception,
                    "Socket Mode geo {Geo} reconnect attempt {Attempt} failed.",
                    Geo,
                    attempt);
            }
        }
    }

    private async Task<Generation> ConnectAsync(CancellationToken cancellationToken)
    {
        Generation generation;
        lock (_sync)
        {
            ThrowIfStopping();
            generation = new Generation(++_generation);
        }

        generation.Connection = _connectionFactory.Create(
            _negotiateUri,
            new SocketConnectionHandlers(
                envelope => DispatchAsync(generation, envelope),
                frame => HandleReady(generation, frame),
                (error, planned) => HandleClosed(generation, error, planned)));

        bool owned;
        lock (_sync)
        {
            owned = !_stopping && _owned.Add(generation.Connection);
        }

        if (!owned)
        {
            await StopAndDisposeAsync(generation.Connection).ConfigureAwait(false);
            throw new OperationCanceledException("Socket Mode geo is stopping.");
        }

        try
        {
            await generation.Connection.StartAsync(cancellationToken).ConfigureAwait(false);

            // StartAsync completes after SocketReady, but the ready callback may still be running.
            if (!TryPromote(generation))
            {
                lock (_sync)
                {
                    ThrowIfStopping();
                }

                throw new IOException("Socket Mode connection closed before it became active.");
            }
        }
        catch
        {
            await ReleaseAsync(generation.Connection).ConfigureAwait(false);
            throw;
        }

        ScheduleRefresh(generation);
        return generation;
    }

    private Task<SocketReplyFrame?> DispatchAsync(Generation generation, SocketActivityEnvelope envelope)
    {
        lock (_sync)
        {
            if (_stopping || (_active != generation && !_retiring.Contains(generation.Id)))
            {
                return Task.FromResult<SocketReplyFrame?>(null);
            }
        }

        return _owner.DispatchAsync(Geo, envelope);
    }

    private void HandleReady(Generation generation, SocketReadyFrame frame)
    {
        if (TryPromote(generation))
        {
            _owner.OnGeoReady(Geo, frame);
        }
    }

    private bool TryPromote(Generation generation)
    {
        lock (_sync)
        {
            if (_stopping || generation.IsClosed || generation.Id != _generation)
            {
                return false;
            }

            if (_active is Generation previous && previous != generation)
            {
                _retiring.Add(previous.Id);
            }

            _active = generation;
            return true;
        }
    }

    private void HandleClosed(Generation generation, Exception? error, bool planned)
    {
        bool disconnected = false;

        lock (_sync)
        {
            generation.IsClosed = true;
            _retiring.Remove(generation.Id);

            if (_active == generation)
            {
                _active = null;
                _refreshTimer?.Dispose();
                _refreshTimer = null;
                disconnected = !planned && !_stopping;
                _disconnected |= disconnected;
            }

            generation.Closed.TrySetResult(new CloseReason(error, planned));
        }

        if (disconnected)
        {
            _logger.LogWarning(error, "Socket Mode geo {Geo} disconnected; inbound delivery paused.", Geo);
            _owner.OnGeoDisconnected(Geo, error);
        }
    }

    private void ReportReconnected()
    {
        lock (_sync)
        {
            if (!_disconnected || _stopping)
            {
                return;
            }

            _disconnected = false;
        }

        _logger.LogInformation("Socket Mode geo {Geo} reconnected; inbound delivery resumed.", Geo);
        _owner.OnGeoReconnected(Geo);
    }

    private void ScheduleRefresh(Generation generation)
    {
        if (generation.Connection.TokenLifetime is not TimeSpan lifetime || lifetime <= TimeSpan.Zero)
        {
            return;
        }

        TimeSpan delay = lifetime - _owner.TokenRefreshMargin;
        if (delay < MinimumRefreshDelay)
        {
            delay = MinimumRefreshDelay;
        }

        lock (_sync)
        {
            if (_stopping || _active != generation)
            {
                return;
            }

            _refreshTimer?.Dispose();
            _refreshTimer = _timeProvider.CreateTimer(
                _ => RequestRotation(generation),
                null,
                delay,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void RequestRotation(Generation generation)
    {
        lock (_sync)
        {
            if (!_stopping && _active == generation)
            {
                generation.Closed.TrySetResult(new CloseReason(null, Planned: true));
            }
        }
    }

    private void StartRetirement(Generation previous)
    {
        Task retirement = RetireAsync(previous);
        lock (_sync)
        {
            if (!retirement.IsCompleted)
            {
                _retirements.Add(retirement);
            }
        }

        _ = retirement.ContinueWith(
            completed =>
            {
                lock (_sync)
                {
                    _retirements.Remove(completed);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Retirement runs in the background; failures are logged so they cannot fault the supervisor.")]
    private async Task RetireAsync(Generation previous)
    {
        try
        {
            TimeSpan handoff = _owner.HandoffWindow;
            await Task.Delay(
                handoff > TimeSpan.Zero ? handoff : TimeSpan.Zero,
                _timeProvider,
                _stopToken).ConfigureAwait(false);

            lock (_sync)
            {
                _retiring.Remove(previous.Id);
            }

            await ReleaseAsync(previous.Connection).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_stopToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Socket Mode geo {Geo} failed to retire a replaced connection.", Geo);
        }
    }

    private async Task StopCoreAsync()
    {
        // Leave the caller's lock before cancelling and stopping connections.
        await Task.Yield();
        await _stopSource.CancelAsync().ConfigureAwait(false);

        ISocketConnection[] connections;
        Task[] retirements;
        Task? supervisor;
        lock (_sync)
        {
            _active = null;
            _retiring.Clear();
            _refreshTimer?.Dispose();
            _refreshTimer = null;
            connections = [.. _owned];
            _owned.Clear();
            retirements = [.. _retirements];
            supervisor = _supervisor;
        }

        try
        {
            await Task.WhenAll(connections.Select(StopAndDisposeAsync)).ConfigureAwait(false);
        }
        finally
        {
            await Task.WhenAll(retirements).ConfigureAwait(false);
            if (supervisor is not null)
            {
                await supervisor.ConfigureAwait(false);
            }
        }
    }

    private async Task ReleaseAsync(ISocketConnection connection)
    {
        lock (_sync)
        {
            if (!_owned.Remove(connection))
            {
                return;
            }
        }

        await StopAndDisposeAsync(connection).ConfigureAwait(false);
    }

    private static async Task StopAndDisposeAsync(ISocketConnection connection)
    {
        try
        {
            await connection.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private TimeSpan GetRetryDelay(Exception? error, int attempt)
    {
        TimeSpan delay = _owner.GetRetryAfter(error) ?? _owner.GetBackoffDelay(attempt);
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private TimeSpan RemainingBudget(TimeSpan budget, long startedAt)
    {
        TimeSpan remaining = budget - _timeProvider.GetElapsedTime(startedAt);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void ThrowIfStopping()
    {
        if (_stopping)
        {
            throw new OperationCanceledException("Socket Mode geo is stopping.");
        }
    }

    private sealed class Generation(long id)
    {
        internal long Id { get; } = id;

        internal ISocketConnection Connection { get; set; } = null!;

        internal TaskCompletionSource<CloseReason> Closed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal bool IsClosed { get; set; }
    }

    private sealed record CloseReason(Exception? Error, bool Planned);
}
