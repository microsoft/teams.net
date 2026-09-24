// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class GeoSocketTests
{
    private static readonly Uri NegotiateUri = new("https://botapi.skype.com/amer/v3/websockets/connect");

    [Fact]
    public async Task StartAsync_CompletesOnlyAfterSocketReady()
    {
        Harness harness = new();
        FakeConnection connection = harness.Factory.Enqueue();

        Task start = harness.Socket.StartAsync();
        await connection.Started.Task;
        Assert.False(start.IsCompleted);

        connection.Ready("initial");
        await start;

        Assert.NotNull(await connection.Activity("a1"));
        Assert.Equal(["initial"], harness.Owner.ReadyIds);
        Assert.Equal(["a1"], harness.Owner.Dispatched);
        Assert.Empty(harness.Owner.Disconnections);
    }

    [Fact]
    public async Task StartAsync_RetriesWithBackoffWithinBudget()
    {
        Harness harness = new() { Backoff = _ => TimeSpan.FromSeconds(2) };
        FakeConnection failed = harness.Factory.Enqueue(startError: new IOException("first"));
        FakeConnection retry = harness.Factory.Enqueue();

        Task start = harness.Socket.StartAsync();
        await harness.Time.WaitForTimerAsync(TimeSpan.FromSeconds(2));
        harness.Time.Advance(TimeSpan.FromSeconds(2));
        await retry.Started.Task;
        retry.Ready("retry");
        await start;

        Assert.Equal(1, failed.StopCount);
        Assert.Equal(1, failed.DisposeCount);
        Assert.Null(await failed.Activity("stale"));
        Assert.Equal(["retry"], harness.Owner.ReadyIds);
    }

    [Fact]
    public async Task StartAsync_PrefersRetryAfterOverBackoff()
    {
        Harness harness = new() { Backoff = _ => TimeSpan.FromSeconds(2) };
        harness.Factory.Enqueue(startError: new SocketModeNegotiateException(
            HttpStatusCode.TooManyRequests,
            TimeSpan.FromSeconds(7)));
        FakeConnection retry = harness.Factory.Enqueue();

        Task start = harness.Socket.StartAsync();
        await harness.Time.WaitForTimerAsync(TimeSpan.FromSeconds(7));
        Assert.False(harness.Time.HasTimerDueIn(TimeSpan.FromSeconds(2)));

        harness.Time.Advance(TimeSpan.FromSeconds(7));
        await retry.Started.Task;
        retry.Ready("retry");
        await start;
    }

    [Fact]
    public async Task StartAsync_FailsWhenPendingAttemptExceedsBudget()
    {
        Harness harness = new() { StartupTimeout = TimeSpan.FromSeconds(5) };
        FakeConnection connection = harness.Factory.Enqueue();

        Task start = harness.Socket.StartAsync();
        await connection.Started.Task;
        await harness.Time.WaitForTimerAsync(TimeSpan.FromSeconds(5));
        harness.Time.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<TimeoutException>(() => start);
        Assert.Equal(1, harness.Factory.CreateCount);
        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    public async Task StartAsync_ZeroBudgetMakesOneAttemptWithoutRetry()
    {
        Harness harness = new() { StartupTimeout = TimeSpan.Zero };
        harness.Factory.Enqueue(startError: new IOException("failed"));

        IOException error = await Assert.ThrowsAsync<IOException>(() => harness.Socket.StartAsync());

        Assert.Equal("failed", error.Message);
        Assert.Equal(1, harness.Factory.CreateCount);
    }

    [Fact]
    public async Task UnexpectedClose_ReportsDisconnectedThenReconnected()
    {
        Harness harness = new() { Backoff = _ => TimeSpan.FromSeconds(3) };
        FakeConnection initial = harness.Factory.Enqueue();
        FakeConnection replacement = harness.Factory.Enqueue();
        await harness.StartReadyAsync(initial);

        IOException dropped = new("dropped");
        initial.Close(dropped);

        Assert.Same(dropped, Assert.Single(harness.Owner.Disconnections));
        Assert.Null(await initial.Activity("after-close"));
        await harness.Time.WaitForTimerAsync(TimeSpan.FromSeconds(3));
        harness.Time.Advance(TimeSpan.FromSeconds(3));
        await replacement.Started.Task;
        replacement.Ready("replacement");
        await WaitUntilAsync(() => harness.Owner.Reconnections == 1);

        Assert.Equal(1, initial.DisposeCount);
        Assert.NotNull(await replacement.Activity("resumed"));
        Assert.DoesNotContain("after-close", harness.Owner.Dispatched);
    }

    [Fact]
    public async Task Rotation_IsMakeBeforeBreakWithHandoff()
    {
        Harness harness = new();
        FakeConnection initial = harness.Factory.Enqueue(TimeSpan.FromSeconds(10));
        FakeConnection replacement = harness.Factory.Enqueue();
        await harness.StartReadyAsync(initial);

        harness.Time.Advance(TimeSpan.FromSeconds(5));
        await replacement.Started.Task;
        Assert.NotNull(await initial.Activity("during-replacement-startup"));
        Assert.Equal(0, initial.StopCount);

        replacement.Ready("replacement");
        await harness.Time.WaitForTimerAsync(harness.Owner.HandoffWindow);
        Assert.NotNull(await initial.Activity("retiring"));
        Assert.NotNull(await replacement.Activity("active"));
        Assert.Equal(0, initial.StopCount);

        harness.Time.Advance(harness.Owner.HandoffWindow);
        await initial.Disposed.Task;

        Assert.Equal(1, initial.StopCount);
        Assert.Null(await initial.Activity("retired"));
        Assert.DoesNotContain("retired", harness.Owner.Dispatched);
        Assert.Empty(harness.Owner.Disconnections);
        Assert.Equal(0, harness.Owner.Reconnections);
        Assert.Equal(2, harness.Owner.ReadyIds.Count);
    }

    [Fact]
    public async Task Rotation_UsesOneSecondMinimumDelay()
    {
        Harness harness = new() { TokenRefreshMargin = TimeSpan.FromSeconds(30) };
        FakeConnection initial = harness.Factory.Enqueue(TimeSpan.FromSeconds(10));
        FakeConnection replacement = harness.Factory.Enqueue();
        await harness.StartReadyAsync(initial);

        Assert.True(harness.Time.HasTimerDueIn(TimeSpan.FromSeconds(1)));
        harness.Time.Advance(TimeSpan.FromSeconds(1));
        await replacement.Started.Task;
    }

    [Fact]
    public async Task TokenLifetimeAbsent_DoesNotScheduleRotation()
    {
        Harness harness = new();
        FakeConnection connection = harness.Factory.Enqueue(tokenLifetime: null);
        await harness.StartReadyAsync(connection);

        Assert.Equal(0, harness.Time.ScheduledTimerCount);
        harness.Time.Advance(TimeSpan.FromDays(1));

        Assert.Equal(1, harness.Factory.CreateCount);
    }

    [Fact]
    public async Task PredecessorDropDuringRotation_ReportsOutageUntilReplacementReady()
    {
        Harness harness = new();
        FakeConnection initial = harness.Factory.Enqueue(TimeSpan.FromSeconds(10));
        FakeConnection replacement = harness.Factory.Enqueue();
        await harness.StartReadyAsync(initial);

        harness.Time.Advance(TimeSpan.FromSeconds(5));
        await replacement.Started.Task;
        initial.Close(new IOException("dropped"));

        Assert.Single(harness.Owner.Disconnections);
        Assert.Null(await initial.Activity("dropped"));

        replacement.Ready("replacement");
        await WaitUntilAsync(() => harness.Owner.Reconnections == 1);
        await harness.Time.WaitForTimerAsync(harness.Owner.HandoffWindow);
        harness.Time.Advance(harness.Owner.HandoffWindow);
        await initial.Disposed.Task;

        Assert.Equal(1, initial.DisposeCount);
    }

    [Fact]
    public async Task RepeatedRotation_ReleasesEveryPredecessorOnce()
    {
        Harness harness = new() { HandoffWindow = TimeSpan.Zero };
        FakeConnection[] connections =
        [
            harness.Factory.Enqueue(TimeSpan.FromSeconds(10)),
            harness.Factory.Enqueue(TimeSpan.FromSeconds(10)),
            harness.Factory.Enqueue(TimeSpan.FromSeconds(10)),
            harness.Factory.Enqueue(TimeSpan.FromSeconds(10)),
        ];
        await harness.StartReadyAsync(connections[0]);

        for (int i = 1; i < connections.Length; i++)
        {
            await harness.Time.WaitForTimerAsync(TimeSpan.FromSeconds(5));
            harness.Time.Advance(TimeSpan.FromSeconds(5));
            await connections[i].Started.Task;
            connections[i].Ready($"gen-{i}");
            await connections[i - 1].Disposed.Task;
        }

        Assert.All(connections[..^1], connection => Assert.Equal(1, connection.DisposeCount));
        Assert.Equal(0, connections[^1].DisposeCount);
        Assert.Empty(harness.Owner.Disconnections);

        await harness.Socket.StopAsync();
        Assert.All(connections, connection => Assert.Equal(1, connection.DisposeCount));
    }

    [Fact]
    public async Task StopAsync_DuringStartup_CancelsAndDisposesOnce()
    {
        Harness harness = new();
        FakeConnection connection = harness.Factory.Enqueue();

        Task start = harness.Socket.StartAsync();
        await connection.Started.Task;
        await harness.Socket.StopAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        await harness.Socket.StopAsync();
        await harness.Socket.DisposeAsync();
        await harness.Socket.DisposeAsync();

        Assert.Equal(1, connection.StopCount);
        Assert.Equal(1, connection.DisposeCount);
        Assert.Empty(harness.Owner.Disconnections);
    }

    [Fact]
    public async Task StartAsync_CallerCancellationInterruptsRetryDelay()
    {
        Harness harness = new() { Backoff = _ => TimeSpan.FromMinutes(1), StartupTimeout = TimeSpan.FromMinutes(5) };
        FakeConnection failed = harness.Factory.Enqueue(startError: new IOException("failed"));
        using CancellationTokenSource cancellation = new();

        Task start = harness.Socket.StartAsync(cancellation.Token);
        await harness.Time.WaitForTimerAsync(TimeSpan.FromMinutes(1));
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Equal(1, harness.Factory.CreateCount);
        Assert.Equal(1, failed.DisposeCount);
    }

    [Fact]
    public async Task StopAsync_DuringReconnectDelay_CancelsWithoutNewGeneration()
    {
        Harness harness = new() { Backoff = _ => TimeSpan.FromMinutes(1) };
        FakeConnection initial = harness.Factory.Enqueue();
        await harness.StartReadyAsync(initial);

        initial.Close(new IOException("dropped"));
        await harness.Time.WaitForTimerAsync(TimeSpan.FromMinutes(1));
        await harness.Socket.StopAsync();
        harness.Time.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(1, harness.Factory.CreateCount);
        Assert.Equal(1, initial.DisposeCount);
        Assert.Equal(0, harness.Owner.Reconnections);
    }

    [Fact]
    public async Task StopAsync_DuringRotation_DisposesActiveAndConnectingOnce()
    {
        Harness harness = new();
        FakeConnection active = harness.Factory.Enqueue(TimeSpan.FromSeconds(10));
        FakeConnection connecting = harness.Factory.Enqueue();
        await harness.StartReadyAsync(active);

        harness.Time.Advance(TimeSpan.FromSeconds(5));
        await connecting.Started.Task;
        await harness.Socket.StopAsync();

        Assert.Equal(1, active.StopCount);
        Assert.Equal(1, active.DisposeCount);
        Assert.Equal(1, connecting.StopCount);
        Assert.Equal(1, connecting.DisposeCount);
        Assert.Empty(harness.Owner.Disconnections);
        Assert.Null(await active.Activity("stopped"));
    }

    [Fact]
    public async Task StopAsync_DuringHandoff_DisposesActiveAndRetiringOnce()
    {
        Harness harness = new() { HandoffWindow = TimeSpan.FromMinutes(1) };
        FakeConnection retiring = harness.Factory.Enqueue(TimeSpan.FromSeconds(10));
        FakeConnection active = harness.Factory.Enqueue();
        await harness.StartReadyAsync(retiring);

        harness.Time.Advance(TimeSpan.FromSeconds(5));
        await active.Started.Task;
        active.Ready("replacement");
        await harness.Time.WaitForTimerAsync(TimeSpan.FromMinutes(1));
        await harness.Socket.StopAsync();
        harness.Time.Advance(TimeSpan.FromMinutes(1));

        Assert.Equal(1, retiring.StopCount);
        Assert.Equal(1, retiring.DisposeCount);
        Assert.Equal(1, active.StopCount);
        Assert.Equal(1, active.DisposeCount);
        Assert.Empty(harness.Owner.Disconnections);
    }

    [Fact]
    public async Task StopAsync_LogsConnectionCleanupFailuresAndStillDisposes()
    {
        Harness harness = new();
        FakeConnection connection = harness.Factory.Enqueue();
        connection.StopError = new IOException("stop failed");
        connection.DisposeError = new IOException("dispose failed");
        await harness.StartReadyAsync(connection);

        await harness.Socket.StopAsync();

        Assert.Equal(1, connection.StopCount);
        Assert.Equal(1, connection.DisposeCount);
        Assert.Equal(
            [connection.StopError, connection.DisposeError],
            harness.Logger.Warnings.Select(warning => warning.Exception));
    }

    [Fact]
    public async Task Retirement_LogsDisposeFailureAndKeepsReplacementActive()
    {
        Harness harness = new() { HandoffWindow = TimeSpan.FromSeconds(5) };
        FakeConnection retiring = harness.Factory.Enqueue(TimeSpan.FromSeconds(10));
        FakeConnection active = harness.Factory.Enqueue();
        retiring.DisposeError = new IOException("dispose failed");
        await harness.StartReadyAsync(retiring);

        harness.Time.Advance(TimeSpan.FromSeconds(5));
        await active.Started.Task;
        active.Ready("replacement");
        await harness.Time.WaitForTimerAsync(TimeSpan.FromSeconds(5));
        harness.Time.Advance(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => harness.Logger.Warnings.Length > 0);

        Assert.Same(retiring.DisposeError, Assert.Single(harness.Logger.Warnings).Exception);
        Assert.NotNull(await active.Activity("after-retirement"));

        await harness.Socket.StopAsync();

        Assert.Equal(1, retiring.DisposeCount);
        Assert.Equal(1, active.DisposeCount);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException("The asynchronous test condition was not met.");
            }

            await Task.Yield();
        }
    }

    private sealed class Harness
    {
        private GeoSocket? _socket;

        internal ManualTimeProvider Time { get; } = new();

        internal FakeOwner Owner { get; } = new();

        internal FakeConnectionFactory Factory { get; } = new();

        internal RecordingLogger Logger { get; } = new();

        internal TimeSpan StartupTimeout { init => Owner.StartupTimeout = value; }

        internal TimeSpan TokenRefreshMargin { init => Owner.TokenRefreshMargin = value; }

        internal TimeSpan HandoffWindow { init => Owner.HandoffWindow = value; }

        internal Func<int, TimeSpan> Backoff { init => Owner.Backoff = value; }

        internal GeoSocket Socket =>
            _socket ??= new GeoSocket(Owner, "amer", NegotiateUri, Factory, Logger, Time);

        internal async Task StartReadyAsync(FakeConnection connection)
        {
            Task start = Socket.StartAsync();
            await connection.Started.Task;
            connection.Ready("initial");
            await start;
        }
    }

    private sealed class FakeOwner : IGeoSocketOwner
    {
        private readonly object _sync = new();
        private readonly List<string> _dispatched = [];
        private readonly List<string> _readyIds = [];
        private readonly List<Exception?> _disconnections = [];
        private int _reconnections;

        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan TokenRefreshMargin { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan HandoffWindow { get; set; } = TimeSpan.FromSeconds(5);

        internal Func<int, TimeSpan> Backoff { get; set; } = _ => TimeSpan.FromSeconds(1);

        internal List<string> Dispatched { get { lock (_sync) { return [.. _dispatched]; } } }

        internal List<string> ReadyIds { get { lock (_sync) { return [.. _readyIds]; } } }

        internal List<Exception?> Disconnections { get { lock (_sync) { return [.. _disconnections]; } } }

        internal int Reconnections => Volatile.Read(ref _reconnections);

        public TimeSpan? GetRetryAfter(Exception? error)
            => (error as SocketModeNegotiateException)?.RetryAfter;

        public TimeSpan GetBackoffDelay(int attempt) => Backoff(attempt);

        public Task<SocketReplyFrame?> DispatchAsync(string geo, SocketActivityEnvelope envelope)
        {
            lock (_sync)
            {
                _dispatched.Add(envelope.EnvelopeId!);
            }

            return Task.FromResult<SocketReplyFrame?>(new SocketReplyFrame { Status = 200 });
        }

        public void OnGeoReady(string geo, SocketReadyFrame frame)
        {
            lock (_sync)
            {
                _readyIds.Add(frame.ConnectionId!);
            }
        }

        public void OnGeoDisconnected(string geo, Exception? error)
        {
            lock (_sync)
            {
                _disconnections.Add(error);
            }
        }

        public void OnGeoReconnected(string geo) => Interlocked.Increment(ref _reconnections);
    }

    private sealed class FakeConnectionFactory : ISocketConnectionFactory
    {
        private readonly Queue<FakeConnection> _pending = [];
        private int _createCount;

        internal int CreateCount => Volatile.Read(ref _createCount);

        internal FakeConnection Enqueue(TimeSpan? tokenLifetime = null, Exception? startError = null)
        {
            FakeConnection connection = new(tokenLifetime, startError);
            lock (_pending)
            {
                _pending.Enqueue(connection);
            }

            return connection;
        }

        public ISocketConnection Create(Uri negotiateUri, SocketConnectionHandlers handlers)
        {
            Interlocked.Increment(ref _createCount);
            FakeConnection connection;
            lock (_pending)
            {
                connection = _pending.Dequeue();
            }

            connection.Handlers = handlers;
            return connection;
        }
    }

    private sealed class FakeConnection(TimeSpan? tokenLifetime, Exception? startError) : ISocketConnection
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _closed;
        private int _stopCount;
        private int _disposeCount;

        public TimeSpan? TokenLifetime { get; } = tokenLifetime;

        internal SocketConnectionHandlers Handlers { get; set; } = null!;

        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal int StopCount => Volatile.Read(ref _stopCount);

        internal int DisposeCount => Volatile.Read(ref _disposeCount);

        internal Exception? StopError { get; set; }

        internal Exception? DisposeError { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            if (startError is not null)
            {
                throw startError;
            }

            await _ready.Task.WaitAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _stopCount);
            _ready.TrySetCanceled(CancellationToken.None);
            RaiseClosed(null, planned: true);
            return StopError is null ? Task.CompletedTask : Task.FromException(StopError);
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            Disposed.TrySetResult();
            return DisposeError is null ? ValueTask.CompletedTask : ValueTask.FromException(DisposeError);
        }

        internal void Ready(string connectionId)
        {
            Handlers.OnReady(new SocketReadyFrame { ConnectionId = connectionId });
            _ready.TrySetResult();
        }

        internal void Close(Exception error) => RaiseClosed(error, planned: false);

        internal Task<SocketReplyFrame?> Activity(string envelopeId)
            => Handlers.OnActivity(new SocketActivityEnvelope { EnvelopeId = envelopeId });

        private void RaiseClosed(Exception? error, bool planned)
        {
            if (Interlocked.Exchange(ref _closed, 1) == 0)
            {
                Handlers.OnClosed(error, planned);
            }
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<(string Message, Exception? Exception)> _warnings = [];

        internal (string Message, Exception? Exception)[] Warnings
        {
            get
            {
                lock (_warnings)
                {
                    return [.. _warnings];
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                lock (_warnings)
                {
                    _warnings.Add((formatter(state, exception), exception));
                }
            }
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly object _sync = new();
        private readonly List<ManualTimer> _timers = [];
        private long _now;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        internal int ScheduledTimerCount
        {
            get
            {
                lock (_sync)
                {
                    return _timers.Count(timer => timer.Due is not null);
                }
            }
        }

        public override long GetTimestamp()
        {
            lock (_sync)
            {
                return _now;
            }
        }

        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ManualTimer timer = new(this, callback, state);
            timer.Change(dueTime, period);
            lock (_sync)
            {
                _timers.Add(timer);
            }

            return timer;
        }

        internal bool HasTimerDueIn(TimeSpan dueIn)
        {
            lock (_sync)
            {
                return _timers.Any(timer => timer.Due == _now + dueIn.Ticks);
            }
        }

        internal Task WaitForTimerAsync(TimeSpan dueIn) => WaitUntilAsync(() => HasTimerDueIn(dueIn));

        internal void Advance(TimeSpan amount)
        {
            List<ManualTimer> due;
            lock (_sync)
            {
                _now += amount.Ticks;
                due = [.. _timers.Where(timer => timer.Due <= _now)];
                foreach (ManualTimer timer in due)
                {
                    timer.Due = null;
                }
            }

            foreach (ManualTimer timer in due)
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
        {
            internal long? Due { get; set; }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (owner._sync)
                {
                    Due = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime.Ticks;
                }

                return true;
            }

            public void Dispose()
            {
                lock (owner._sync)
                {
                    Due = null;
                    owner._timers.Remove(this);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            internal void Fire() => callback(state);
        }
    }
}
