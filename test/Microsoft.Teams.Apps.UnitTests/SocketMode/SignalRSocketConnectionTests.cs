// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SignalRSocketConnectionTests
{
    private static readonly Uri NegotiateUri =
        new("https://botapi.skype.com/amer/v3/websockets/connect");
    private static readonly Uri SignalRUri =
        new("https://signalr.example.test/client");

    [Fact]
    public async Task StartAsync_NegotiatesCreatesAndStartsConnection()
    {
        TestHarness harness = CreateHarness(expiresIn: 3600);

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame
        {
            BotKey = "bot-id",
            ConnectionId = "connection-id",
        });
        await start;

        Assert.Equal(1, harness.Negotiator.CallCount);
        Assert.Equal(NegotiateUri, harness.Negotiator.NegotiateUri);
        Assert.Equal(SignalRUri, harness.SignalRFactory.Url);
        Assert.Equal("signalr-token", harness.SignalRFactory.AccessToken);
        Assert.Equal(TimeSpan.FromSeconds(15), harness.SignalRFactory.KeepAliveInterval);
        Assert.Equal(TimeSpan.FromSeconds(30), harness.SignalRFactory.ServerTimeout);
        Assert.Equal(TimeSpan.FromHours(1), harness.Connection.TokenLifetime);
        Assert.Equal(1, harness.SignalR.StartCount);
        Assert.Single(harness.ReadyFrames);
    }

    [Fact]
    public async Task StartAsync_DoesNotCompleteBeforeSocketReady()
    {
        TestHarness harness = CreateHarness();

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;

        Assert.False(start.IsCompleted);

        harness.SignalR.Ready(new SocketReadyFrame());
        await start;
    }

    [Fact]
    public async Task StartAsync_IgnoresDuplicateSocketReady()
    {
        TestHarness harness = CreateHarness();

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame { ConnectionId = "first" });
        harness.SignalR.Ready(new SocketReadyFrame { ConnectionId = "second" });
        await start;

        SocketReadyFrame ready = Assert.Single(harness.ReadyFrames);
        Assert.Equal("first", ready.ConnectionId);
    }

    [Fact]
    public async Task StartAsync_SettlesBeforeReadyObserverThrows()
    {
        FakeNegotiator negotiator = new(SuccessfulNegotiation());
        FakeSignalRClientConnection signalR = new();
        FakeSignalRConnectionFactory signalRFactory = new(signalR);
        SocketConnectionHandlers handlers = new(
            _ => Task.FromResult<SocketReplyFrame?>(null),
            _ => throw new InvalidOperationException("observer failed"),
            (_, _) => { });
        SignalRSocketConnection connection = CreateConnection(
            negotiator,
            signalRFactory,
            handlers);

        Task start = connection.StartAsync(CancellationToken.None);
        await signalR.Started.Task;
        signalR.Ready(new SocketReadyFrame());
        await start;

        Assert.True(start.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ActivityHandler_ReturnsClientResult()
    {
        SocketReplyFrame expected = new() { Status = 202 };
        TestHarness harness = CreateHarness(
            onActivity: _ => Task.FromResult<SocketReplyFrame?>(expected));

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        SocketReplyFrame? actual = await harness.SignalR.Activity(
            new SocketActivityEnvelope { EnvelopeId = "env-1" });

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task ActivityHandler_AllowsNullClientResult()
    {
        TestHarness harness = CreateHarness();

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        SocketReplyFrame? actual = await harness.SignalR.Activity(
            new SocketActivityEnvelope());

        Assert.Null(actual);
    }

    [Fact]
    public async Task CloseBeforeReady_FailsStartupAndNotifiesClosed()
    {
        TestHarness harness = CreateHarness();
        IOException expected = new("connection lost");

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Close(expected);

        IOException actual = await Assert.ThrowsAsync<IOException>(() => start);
        Assert.Same(expected, actual);
        (Exception? error, bool planned) = Assert.Single(harness.CloseEvents);
        Assert.Same(expected, error);
        Assert.False(planned);
        Assert.Equal(1, harness.SignalR.StopCount);
        Assert.Equal(1, harness.SignalR.DisposeCount);
    }

    [Fact]
    public async Task CloseAfterReady_NotifiesClosedOnce()
    {
        TestHarness harness = CreateHarness();
        IOException expected = new("connection lost");

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        harness.SignalR.Close(expected);
        harness.SignalR.Close(new IOException("duplicate"));

        (Exception? error, bool planned) = Assert.Single(harness.CloseEvents);
        Assert.Same(expected, error);
        Assert.False(planned);
    }

    [Fact]
    public async Task ReadinessTimeout_StopsConnection()
    {
        TestHarness harness = CreateHarness(
            readinessTimeout: TimeSpan.FromMilliseconds(50));

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;

        TimeoutException exception =
            await Assert.ThrowsAsync<TimeoutException>(() => start);

        Assert.Contains("readiness timed out", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, harness.SignalR.StopCount);
        Assert.Equal(1, harness.SignalR.DisposeCount);

        await harness.Connection.DisposeAsync();
        Assert.Equal(1, harness.SignalR.DisposeCount);
    }

    [Fact]
    public async Task CallerCancellation_StopsStartup()
    {
        TestHarness harness = CreateHarness();
        using CancellationTokenSource cancellationSource = new();

        Task start = harness.Connection.StartAsync(cancellationSource.Token);
        await harness.SignalR.Started.Task;
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Equal(1, harness.SignalR.StopCount);
        Assert.Equal(1, harness.SignalR.DisposeCount);
        (Exception? error, bool planned) = Assert.Single(harness.CloseEvents);
        Assert.Null(error);
        Assert.True(planned);
    }

    [Fact]
    public async Task StopAsync_InterruptsReadinessWaitAndIsIdempotent()
    {
        TestHarness harness = CreateHarness();

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;

        await harness.Connection.StopAsync();
        await harness.Connection.StopAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Equal(1, harness.SignalR.StopCount);
    }

    [Fact]
    public async Task StopAsync_CallerCancellationDoesNotPoisonDispose()
    {
        TestHarness harness = CreateHarness();
        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        harness.SignalR.StopCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource cancellationSource = new();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Connection.StopAsync(cancellationSource.Token));

        ValueTask dispose = harness.Connection.DisposeAsync();
        Assert.False(dispose.IsCompleted);

        harness.SignalR.StopCompletion.TrySetResult();
        await dispose;

        Assert.Equal(1, harness.SignalR.StopCount);
        Assert.Equal(1, harness.SignalR.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_StopsAndDisposesUnderlyingConnectionOnce()
    {
        TestHarness harness = CreateHarness();

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        await harness.Connection.DisposeAsync();
        await harness.Connection.DisposeAsync();

        Assert.Equal(1, harness.SignalR.StopCount);
        Assert.Equal(1, harness.SignalR.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_DisposesConnectionCreatedAfterShutdown()
    {
        DeferredNegotiator negotiator = new();
        FakeSignalRClientConnection signalR = new();
        FakeSignalRConnectionFactory signalRFactory = new(signalR);
        SignalRSocketConnection connection = CreateConnection(
            negotiator,
            signalRFactory,
            DefaultHandlers());

        Task start = connection.StartAsync(CancellationToken.None);
        await negotiator.Started.Task;
        await connection.DisposeAsync();

        negotiator.Complete(SuccessfulNegotiation());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.Equal(0, signalR.StartCount);
        Assert.Equal(0, signalR.StopCount);
        Assert.Equal(1, signalR.DisposeCount);
    }

    [Fact]
    public async Task StartAsync_CannotBeCalledTwice()
    {
        TestHarness harness = CreateHarness();

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Connection.StartAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task StartAsync_LeavesTokenLifetimeNullForNonpositiveExpiry(
        int expiresIn)
    {
        TestHarness harness = CreateHarness(expiresIn);

        Task start = harness.Connection.StartAsync(CancellationToken.None);
        await harness.SignalR.Started.Task;
        harness.SignalR.Ready(new SocketReadyFrame());
        await start;

        Assert.Null(harness.Connection.TokenLifetime);
    }

    [Fact]
    public void Factory_CreatesDistinctConnectionGenerations()
    {
        FakeNegotiator negotiator = new(SuccessfulNegotiation());
        FakeSignalRConnectionFactory signalRFactory =
            new(new FakeSignalRClientConnection());
        SignalRSocketConnectionFactory factory = new(
            negotiator,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            NullLogger.Instance,
            signalRFactory.Create);
        SocketConnectionHandlers handlers = DefaultHandlers();

        ISocketConnection first = factory.Create(NegotiateUri, handlers);
        ISocketConnection second = factory.Create(NegotiateUri, handlers);

        Assert.NotSame(first, second);
    }

    private static TestHarness CreateHarness(
        int expiresIn = 3600,
        Func<SocketActivityEnvelope, Task<SocketReplyFrame?>>? onActivity = null,
        TimeSpan? readinessTimeout = null)
    {
        FakeNegotiator negotiator = new(SuccessfulNegotiation(expiresIn));
        FakeSignalRClientConnection signalR = new();
        FakeSignalRConnectionFactory signalRFactory = new(signalR);
        List<SocketReadyFrame> readyFrames = [];
        List<(Exception? Error, bool Planned)> closeEvents = [];
        SocketConnectionHandlers handlers = new(
            onActivity ?? (_ => Task.FromResult<SocketReplyFrame?>(null)),
            readyFrames.Add,
            (error, planned) => closeEvents.Add((error, planned)));

        return new TestHarness(
            CreateConnection(
                negotiator,
                signalRFactory,
                handlers,
                readinessTimeout),
            negotiator,
            signalRFactory,
            signalR,
            readyFrames,
            closeEvents);
    }

    private static SignalRSocketConnection CreateConnection(
        ISocketModeNegotiator negotiator,
        FakeSignalRConnectionFactory signalRFactory,
        SocketConnectionHandlers handlers,
        TimeSpan? readinessTimeout = null)
        => new(
            NegotiateUri,
            handlers,
            negotiator,
            signalRFactory.Create,
            readinessTimeout ?? TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            NullLogger.Instance);

    private static SocketModeNegotiateResponse SuccessfulNegotiation(
        int expiresIn = 3600)
        => new()
        {
            Url = SignalRUri.ToString(),
            AccessToken = "signalr-token",
            ExpiresIn = expiresIn,
        };

    private static SocketConnectionHandlers DefaultHandlers()
        => new(
            _ => Task.FromResult<SocketReplyFrame?>(null),
            _ => { },
            (_, _) => { });

    private sealed record TestHarness(
        SignalRSocketConnection Connection,
        FakeNegotiator Negotiator,
        FakeSignalRConnectionFactory SignalRFactory,
        FakeSignalRClientConnection SignalR,
        List<SocketReadyFrame> ReadyFrames,
        List<(Exception? Error, bool Planned)> CloseEvents);

    private sealed class FakeNegotiator(SocketModeNegotiateResponse response)
        : ISocketModeNegotiator
    {
        internal int CallCount { get; private set; }
        internal Uri? NegotiateUri { get; private set; }

        public Task<SocketModeNegotiateResponse> NegotiateAsync(
            Uri negotiateUri,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            NegotiateUri = negotiateUri;
            return Task.FromResult(response);
        }
    }

    private sealed class DeferredNegotiator : ISocketModeNegotiator
    {
        private readonly TaskCompletionSource<SocketModeNegotiateResponse>
            _response = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SocketModeNegotiateResponse> NegotiateAsync(
            Uri negotiateUri,
            CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            return _response.Task;
        }

        internal void Complete(SocketModeNegotiateResponse response)
            => _response.TrySetResult(response);
    }

    private sealed class FakeSignalRConnectionFactory(
        FakeSignalRClientConnection connection)
    {
        internal Uri? Url { get; private set; }
        internal string? AccessToken { get; private set; }
        internal TimeSpan KeepAliveInterval { get; private set; }
        internal TimeSpan ServerTimeout { get; private set; }

        internal ISignalRClientConnection Create(
            Uri url,
            string accessToken,
            TimeSpan keepAliveInterval,
            TimeSpan serverTimeout)
        {
            Url = url;
            AccessToken = accessToken;
            KeepAliveInterval = keepAliveInterval;
            ServerTimeout = serverTimeout;
            return connection;
        }
    }

    private sealed class FakeSignalRClientConnection
        : ISignalRClientConnection
    {
        private Func<SocketActivityEnvelope, Task<SocketReplyFrame?>>?
            _onActivity;
        private Action<SocketReadyFrame>? _onReady;
        private Action<Exception?>? _onClosed;

        internal TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int StartCount { get; private set; }
        internal int StopCount { get; private set; }
        internal int DisposeCount { get; private set; }
        internal TaskCompletionSource? StopCompletion { get; set; }

        public void OnActivity(
            Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> handler)
            => _onActivity = handler;

        public void OnReady(Action<SocketReadyFrame> handler)
            => _onReady = handler;

        public void OnClosed(Action<Exception?> handler)
            => _onClosed = handler;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCount++;
            Started.TrySetResult();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            _onClosed?.Invoke(null);
            return StopCompletion?.Task ?? Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }

        internal Task<SocketReplyFrame?> Activity(
            SocketActivityEnvelope envelope)
            => (_onActivity ?? throw new InvalidOperationException())(envelope);

        internal void Ready(SocketReadyFrame frame)
            => (_onReady ?? throw new InvalidOperationException())(frame);

        internal void Close(Exception? error)
            => (_onClosed ?? throw new InvalidOperationException())(error);
    }
}
