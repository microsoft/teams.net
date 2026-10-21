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
    /// Gets how long a retired connection stays open after its successor is ready, even when nothing is in flight
    /// on it, because the service can still route a few deliveries to it from its socket cache.
    /// </summary>
    TimeSpan RetirementMinimumDrain { get; }

    /// <summary>
    /// Gets how long a retired connection stays open after its last reply so the reply can be sent before closing.
    /// </summary>
    TimeSpan ReplyFlushDelay { get; }

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
/// or a predecessor that is explicitly retiring during a token rotation handoff. A planned rotation names the
/// still-open predecessor on the connect request so the service can stop routing to it, then drains the predecessor:
/// it stays open until nothing is in flight on it and the minimum drain has elapsed, but never past the latest
/// deadline among its in-flight envelopes.
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
    private readonly HashSet<Task> _releases = [];

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
    /// <exception cref="SocketModeNegotiateException">Thrown without retrying when negotiate rejects the bot with HTTP 401 or 403.</exception>
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
    /// Connection cleanup failures are logged rather than thrown.
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
                return await ConnectAsync(replacesConnectionId: null, attemptSource.Token).ConfigureAwait(false);
            }
            catch (Exception exception) when (!startSource.IsCancellationRequested)
            {
                if (SocketModeNegotiateException.IsNonRetryable(exception))
                {
                    throw;
                }

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

                Generation? replacement;
                if (reason.Planned)
                {
                    _logger.LogInformation("Socket Mode geo {Geo} rotating connection before token expiry.", Geo);
                    replacement = await ReconnectAsync(null, replacing: current).ConfigureAwait(false);
                    if (replacement is not null)
                    {
                        StartRetirement(current, replacement);
                    }
                }
                else
                {
                    await ReleaseAsync(current.Connection).ConfigureAwait(false);
                    replacement = await ReconnectAsync(reason.Error, replacing: null).ConfigureAwait(false);
                }

                if (replacement is null)
                {
                    return;
                }

                current = replacement;

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

    /// <param name="previousError">The failure that triggered the reconnect, or <see langword="null"/> for a planned rotation.</param>
    /// <param name="replacing">The generation a planned rotation replaces; <see langword="null"/> after a drop.</param>
    /// <returns>The ready replacement, or <see langword="null"/> when the geo gave up after a non-retryable failure.</returns>
    private async Task<Generation?> ReconnectAsync(Exception? previousError, Generation? replacing)
    {
        Exception? error = previousError;

        for (int attempt = 1; ; attempt++)
        {
            await Task.Delay(GetRetryDelay(error, attempt - 1), _timeProvider, _stopToken).ConfigureAwait(false);

            try
            {
                // Re-evaluated per attempt: once the replaced socket closes (for example at token expiry after
                // repeated 429s), later attempts connect without naming it.
                return await ConnectAsync(GetReplacedConnectionId(replacing), _stopToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!_stopToken.IsCancellationRequested)
            {
                if (SocketModeNegotiateException.IsNonRetryable(exception))
                {
                    StopAfterNonRetryable(exception);
                    return null;
                }

                error = exception;
                _logger.LogWarning(
                    exception,
                    "Socket Mode geo {Geo} reconnect attempt {Attempt} failed.",
                    Geo,
                    attempt);
            }
        }
    }

    /// <summary>
    /// Gives up on this geo: stops every connection it owns, including one still serving during a token rotation,
    /// and reports it disconnected. Other geos are unaffected.
    /// </summary>
    /// <param name="error">The non-retryable failure.</param>
    private void StopAfterNonRetryable(Exception error)
    {
        _logger.LogError(
            error,
            "Socket Mode geo {Geo} reconnect was rejected; inbound delivery for this geo has stopped until the app is restarted.",
            Geo);

        // Not awaited: stopping waits for this supervisor to finish. Marking the geo as stopping happens
        // synchronously, so closing connections cannot report a second disconnect.
        _ = StopAsync();
        _owner.OnGeoDisconnected(Geo, error);
    }

    private async Task<Generation> ConnectAsync(string? replacesConnectionId, CancellationToken cancellationToken)
    {
        Generation generation;
        lock (_sync)
        {
            ThrowIfStopping();
            generation = new Generation(++_generation);
        }

        if (replacesConnectionId is not null)
        {
            _logger.LogDebug(
                "Socket Mode geo {Geo} connecting a replacement for connection {ConnectionId}.",
                Geo,
                replacesConnectionId);
        }

        generation.Connection = _connectionFactory.Create(
            _negotiateUri,
            new SocketConnectionHandlers(
                envelope => DispatchAsync(generation, envelope),
                frame => HandleReady(generation, frame),
                (error, planned) => HandleClosed(generation, error, planned)),
            replacesConnectionId);

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
            // The token lifetime counts from negotiate, which happens inside StartAsync.
            generation.ConnectStartedAt = _timeProvider.GetTimestamp();
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

    private async Task<SocketReplyFrame?> DispatchAsync(Generation generation, SocketActivityEnvelope envelope)
    {
        long envelopeToken;
        lock (_sync)
        {
            if (_stopping || (_active != generation && !_retiring.Contains(generation.Id)))
            {
                return null;
            }

            // Tracked on every generation, not only retiring ones, so work that arrived before a rotation still
            // holds the predecessor open while it drains.
            envelopeToken = generation.BeginEnvelope(_timeProvider.GetTimestamp(), GetEnvelopeDeadline(envelope));
        }

        try
        {
            return await _owner.DispatchAsync(Geo, envelope).ConfigureAwait(false);
        }
        finally
        {
            lock (_sync)
            {
                generation.EndEnvelope(envelopeToken, _timeProvider.GetTimestamp());

                // Arrivals can only extend a drain, so only wake it when this reply lets it close sooner.
                if (generation.DrainReadyAt is long readyAt
                    && generation.DrainWake is TimeSpan scheduledWake
                    && _timeProvider.GetElapsedTime(readyAt) + GetDrainWait(generation, readyAt) < scheduledWake)
                {
                    generation.Signal();
                }
            }
        }
    }

    private void HandleReady(Generation generation, SocketReadyFrame frame)
    {
        lock (_sync)
        {
            generation.ConnectionId ??= frame.ConnectionId;
        }

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
            generation.ReadyAt ??= _timeProvider.GetTimestamp();
            return true;
        }
    }

    private void HandleClosed(Generation generation, Exception? error, bool planned)
    {
        bool disconnected = false;

        lock (_sync)
        {
            generation.IsClosed = true;
            generation.Signal();
            _retiring.Remove(generation.Id);

            if (_active == generation)
            {
                _active = null;
                _refreshTimer?.Dispose();
                _refreshTimer = null;
                disconnected = !planned && !_stopping;
                _disconnected |= disconnected;
            }
        }

        // Report the disconnect before waking the supervisor so a fast reconnect cannot be reported first.
        try
        {
            if (disconnected)
            {
                _logger.LogWarning(error, "Socket Mode geo {Geo} disconnected; inbound delivery paused.", Geo);
                _owner.OnGeoDisconnected(Geo, error);
            }
        }
        finally
        {
            generation.Closed.TrySetResult(new CloseReason(error, planned));
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

        TimeSpan delay = lifetime - _owner.TokenRefreshMargin - _timeProvider.GetElapsedTime(generation.ConnectStartedAt);
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

    private void StartRetirement(Generation previous, Generation replacement)
    {
        Task retirement = RetireAsync(previous, replacement);
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

    /// <summary>
    /// Drains a retired generation, then stops it. It keeps serving until nothing is in flight on it and the minimum
    /// drain has elapsed since its replacement became ready, but never waits past the latest deadline among the
    /// envelopes still in flight. A close by the service (for example
    /// "Replaced by a newer socket.") ends the drain without reconnecting: the replacement is already serving.
    /// </summary>
    private async Task RetireAsync(Generation previous, Generation replacement)
    {
        try
        {
            long readyAt;
            lock (_sync)
            {
                readyAt = replacement.ReadyAt ?? _timeProvider.GetTimestamp();
                previous.DrainReadyAt = readyAt;
            }

            _logger.LogDebug(
                "Socket Mode geo {Geo} draining replaced connection {ConnectionId}.",
                Geo,
                previous.ConnectionId);

            while (true)
            {
                Task changed;
                TimeSpan wait;
                lock (_sync)
                {
                    if (_stopping || previous.IsClosed)
                    {
                        break;
                    }

                    wait = GetDrainWait(previous, readyAt);
                    if (wait <= TimeSpan.Zero)
                    {
                        break;
                    }

                    previous.DrainWake = _timeProvider.GetElapsedTime(readyAt) + wait;
                    changed = previous.Changed;
                }

                try
                {
                    await changed.WaitAsync(wait, _timeProvider, _stopToken).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                }
            }

            int abandoned;
            lock (_sync)
            {
                _retiring.Remove(previous.Id);
                abandoned = previous.InFlightCount;
            }

            if (abandoned > 0)
            {
                _logger.LogDebug(
                    "Socket Mode geo {Geo} closing replaced connection {ConnectionId} with {Count} envelope(s) past their deadline.",
                    Geo,
                    previous.ConnectionId,
                    abandoned);
            }

            await ReleaseAsync(previous.Connection).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_stopToken.IsCancellationRequested)
        {
        }
    }

    /// <returns>How much longer the retired generation must stay open; zero or less when it can close now.</returns>
    private TimeSpan GetDrainWait(Generation previous, long readyAt)
    {
        TimeSpan wait = _owner.RetirementMinimumDrain - _timeProvider.GetElapsedTime(readyAt);
        if (previous.GetLatestInFlightDeadline(_timeProvider) is TimeSpan untilDeadline)
        {
            wait = Max(wait, untilDeadline);
        }
        else if (previous.LastReplyAt is long lastReplyAt)
        {
            wait = Max(wait, _owner.ReplyFlushDelay - _timeProvider.GetElapsedTime(lastReplyAt));
        }

        return wait;
    }

    private async Task StopCoreAsync()
    {
        // Leave the caller's lock before cancelling and stopping connections.
        await Task.Yield();
        await _stopSource.CancelAsync().ConfigureAwait(false);

        ISocketConnection[] connections;
        Task[] retirements;
        Task[] releases;
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
            releases = [.. _releases];
            supervisor = _supervisor;
        }

        try
        {
            await Task.WhenAll(connections.Select(StopAndDisposeAsync)).ConfigureAwait(false);
        }
        finally
        {
            await Task.WhenAll(retirements).ConfigureAwait(false);
            await Task.WhenAll(releases).ConfigureAwait(false);
            if (supervisor is not null)
            {
                await supervisor.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }

    private async Task ReleaseAsync(ISocketConnection connection)
    {
        TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            if (!_owned.Remove(connection))
            {
                return;
            }

            // Registered with the ownership claim so StopAsync can await cleanup it no longer owns.
            _releases.Add(released.Task);
        }

        try
        {
            await StopAndDisposeAsync(connection).ConfigureAwait(false);
        }
        finally
        {
            lock (_sync)
            {
                _releases.Remove(released.Task);
            }

            released.TrySetResult();
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Connection cleanup is best-effort; failures are logged so they cannot mask the caller's outcome.")]
    private async Task StopAndDisposeAsync(ISocketConnection connection)
    {
        try
        {
            await connection.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Socket Mode geo {Geo} failed to stop a connection.", Geo);
        }

        try
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Socket Mode geo {Geo} failed to dispose a connection.", Geo);
        }
    }

    private TimeSpan GetRetryDelay(Exception? error, int attempt)
    {
        TimeSpan delay = _owner.GetRetryAfter(error) ?? _owner.GetBackoffDelay(attempt);
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private string? GetReplacedConnectionId(Generation? replacing)
    {
        if (replacing is null)
        {
            return null;
        }

        lock (_sync)
        {
            return replacing.IsClosed || string.IsNullOrEmpty(replacing.ConnectionId)
                ? null
                : replacing.ConnectionId;
        }
    }

    // A missing or invalid deadlineMs gives no drain budget beyond the minimum drain, matching the other SDKs.
    private static TimeSpan GetEnvelopeDeadline(SocketActivityEnvelope envelope)
        => envelope.DeadlineMs is int deadlineMs && deadlineMs > 0
            ? TimeSpan.FromMilliseconds(deadlineMs)
            : TimeSpan.Zero;

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;

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

    /// <remarks>
    /// Mutable state is guarded by the owning <see cref="GeoSocket"/>'s lock.
    /// </remarks>
    private sealed class Generation(long id)
    {
        private readonly Dictionary<long, (long ArrivedAt, TimeSpan Deadline)> _inFlight = [];
        private TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private long _nextEnvelope;

        internal long Id { get; } = id;

        internal ISocketConnection Connection { get; set; } = null!;

        internal TaskCompletionSource<CloseReason> Closed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal bool IsClosed { get; set; }

        /// <summary>
        /// Gets or sets the server connection ID from this generation's SocketReady frame, exactly as received.
        /// </summary>
        internal string? ConnectionId { get; set; }

        internal long ConnectStartedAt { get; set; }

        internal long? ReadyAt { get; set; }

        internal long? LastReplyAt { get; private set; }

        /// <summary>
        /// Gets or sets when this generation's replacement became ready, once it is draining.
        /// </summary>
        internal long? DrainReadyAt { get; set; }

        /// <summary>
        /// Gets or sets when the drain next re-evaluates, measured from <see cref="DrainReadyAt"/>.
        /// </summary>
        internal TimeSpan? DrainWake { get; set; }

        internal int InFlightCount => _inFlight.Count;

        /// <summary>
        /// Gets a task that completes when <see cref="Signal"/> is next called.
        /// </summary>
        internal Task Changed => _changed.Task;

        internal long BeginEnvelope(long arrivedAt, TimeSpan deadline)
        {
            long token = ++_nextEnvelope;
            _inFlight[token] = (arrivedAt, deadline);
            return token;
        }

        internal void EndEnvelope(long token, long repliedAt)
        {
            if (_inFlight.Remove(token))
            {
                LastReplyAt = repliedAt;
            }
        }

        /// <returns>
        /// The time until the latest deadline among envelopes still in flight, ignoring those already past their
        /// deadline because the service has discarded their replies; <see langword="null"/> when none remain.
        /// </returns>
        internal TimeSpan? GetLatestInFlightDeadline(TimeProvider timeProvider)
        {
            TimeSpan? latest = null;
            foreach ((long arrivedAt, TimeSpan deadline) in _inFlight.Values)
            {
                TimeSpan remaining = deadline - timeProvider.GetElapsedTime(arrivedAt);
                if (remaining > TimeSpan.Zero && (latest is null || remaining > latest))
                {
                    latest = remaining;
                }
            }

            return latest;
        }

        internal void Signal()
        {
            TaskCompletionSource changed = _changed;
            _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            changed.TrySetResult();
        }
    }

    private sealed record CloseReason(Exception? Error, bool Planned);
}

