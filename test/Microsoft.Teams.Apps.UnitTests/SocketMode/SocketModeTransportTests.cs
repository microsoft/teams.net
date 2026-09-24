// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeTransportTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);

    [Fact]
    public async Task StartAsync_ConnectsEveryDefaultGeo()
    {
        Harness harness = new();

        Task start = harness.Transport.StartAsync();
        FakeConnection[] connections = await harness.Factory.WaitForAsync(3);

        Assert.Equal(
            [
                "https://botapi.skype.com/amer/v3/websockets/connect",
                "https://botapi.skype.com/apac/v3/websockets/connect",
                "https://botapi.skype.com/emea/v3/websockets/connect",
            ],
            SortedUris(connections));
        Assert.False(start.IsCompleted);
        Assert.Equal(SocketModeStatus.Connecting, harness.Transport.Status);

        foreach (FakeConnection connection in connections)
        {
            connection.Ready();
        }

        await start;
        Assert.Equal(SocketModeStatus.Ready, harness.Transport.Status);
        Assert.All(harness.Transport.GeoStatuses.Values, status => Assert.Equal(SocketModeStatus.Ready, status));
    }

    [Fact]
    public async Task StartAsync_BuildsGeoUrisFromCustomBaseAndEmptyGeo()
    {
        Harness harness = new(new SocketModeTransportOptions
        {
            NegotiateBaseUri = new Uri("http://localhost:3978/"),
            Geos = ["", " /eu/ "],
        });

        Task start = harness.Transport.StartAsync();
        FakeConnection[] connections = await harness.Factory.WaitForAsync(2);

        Assert.Equal(
            [
                "http://localhost:3978/eu/v3/websockets/connect",
                "http://localhost:3978/v3/websockets/connect",
            ],
            SortedUris(connections));
        foreach (FakeConnection connection in connections)
        {
            connection.Ready();
        }

        await start;
    }

    [Theory]
    [InlineData(new object[] { new string[0] })]
    [InlineData(new object[] { new[] { "amer", "AMER" } })]
    public void Constructor_RejectsInvalidGeos(string[] geos)
    {
        Assert.Throws<ArgumentException>(() => new Harness(new SocketModeTransportOptions { Geos = geos }).Transport);
    }

    [Fact]
    public void Constructor_RejectsNegativeStartupTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Harness(new SocketModeTransportOptions
        {
            StartupTimeout = TimeSpan.FromSeconds(-1),
        }).Transport);
    }

    [Fact]
    public async Task StartAsync_WhenAnyGeoFails_StopsEveryGeoAndThrowsThatFailure()
    {
        IOException failure = new("emea failed");
        Harness harness = new(new SocketModeTransportOptions { StartupTimeout = TimeSpan.Zero });
        harness.Factory.FailStart("emea", failure);

        IOException thrown = await Assert.ThrowsAsync<IOException>(() => harness.Transport.StartAsync());

        Assert.Same(failure, thrown);
        Assert.Equal(SocketModeStatus.Stopped, harness.Transport.Status);
        Assert.All(harness.Factory.Connections, connection => Assert.Equal(1, connection.DisposeCount));
    }

    [Fact]
    public async Task StopAsync_DuringStartup_CancelsStartAndDisposesConnections()
    {
        Harness harness = new();
        Task start = harness.Transport.StartAsync();
        FakeConnection[] connections = await harness.Factory.WaitForAsync(3);

        await harness.Transport.StopAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
        Assert.All(connections, connection => Assert.Equal(1, connection.DisposeCount));
        Assert.Equal(SocketModeStatus.Stopped, harness.Transport.Status);
    }

    [Fact]
    public async Task StopAsync_IsIdempotentAndDisposesEachConnectionOnce()
    {
        Harness harness = new();
        FakeConnection[] connections = await harness.StartReadyAsync();

        await harness.Transport.StopAsync();
        await harness.Transport.StopAsync();
        await harness.Transport.DisposeAsync();

        Assert.All(connections, connection =>
        {
            Assert.Equal(1, connection.StopCount);
            Assert.Equal(1, connection.DisposeCount);
        });
        Assert.Equal(SocketModeStatus.Stopped, harness.Transport.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Transport.StartAsync());
    }

    [Fact]
    public async Task Status_ReportsDisconnectedUntilDroppedGeoRecovers()
    {
        Harness harness = new(new SocketModeTransportOptions { ReconnectDelays = [TimeSpan.Zero] });
        FakeConnection[] connections = await harness.StartReadyAsync();
        FakeConnection amer = connections.Single(connection => connection.Geo == "amer");

        amer.Close(new IOException("dropped"));

        Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.Status);
        Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.GeoStatuses["amer"]);
        Assert.Equal(SocketModeStatus.Ready, harness.Transport.GeoStatuses["emea"]);

        FakeConnection replacement = (await harness.Factory.WaitForAsync(4))[3];
        replacement.Ready();
        await WaitUntilAsync(() => harness.Transport.Status == SocketModeStatus.Ready);
    }

    [Fact]
    public async Task Dispatch_InvokeReturnsHandlerStatusAndBody()
    {
        Harness harness = new() { Dispatch = _ => Task.FromResult(new SocketDispatchResult(201, "created")) };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope("invoke", """{"type":"invoke","id":"a1"}"""));

        Assert.NotNull(reply);
        Assert.Equal("env-1", reply.EnvelopeId);
        Assert.Equal("bot-id", reply.BotKey);
        Assert.Equal(201, reply.Status);
        Assert.Equal("created", reply.Body);
        Assert.Equal(Now.ToUnixTimeMilliseconds(), reply.ReceivedAtUnixMilliseconds);
        Assert.Equal("a1", Assert.Single(harness.Dispatched).Id);
    }

    [Fact]
    public async Task Dispatch_MessageReturnsAcknowledgementWithHandlerStatus()
    {
        Harness harness = new() { Dispatch = _ => Task.FromResult(new SocketDispatchResult(202, "ignored")) };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope("message", """{"type":"message"}"""));

        Assert.NotNull(reply);
        Assert.Equal(202, reply.Status);
        Assert.Null(reply.Body);
    }

    [Fact]
    public async Task Dispatch_ClassifiesInvokeFromActivityWhenEnvelopeTypeIsAbsent()
    {
        Harness harness = new() { Dispatch = _ => Task.FromResult(new SocketDispatchResult(200, "result")) };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope(null, """{"type":"invoke"}"""));

        Assert.Equal("result", reply?.Body);
    }

    [Theory]
    [InlineData("invoke", """{"type":"invoke"}""", true)]
    [InlineData("message", """{"type":"message"}""", false)]
    public async Task Dispatch_HandlerFailureReturns500AndReportsError(string type, string payload, bool hasBody)
    {
        InvalidOperationException failure = new("handler failed");
        List<Exception> reported = [];
        Harness harness = new()
        {
            Dispatch = _ => throw failure,
            OnError = exception =>
            {
                reported.Add(exception);
                return Task.CompletedTask;
            },
        };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope(type, payload));

        Assert.NotNull(reply);
        Assert.Equal(500, reply.Status);
        Assert.Equal(hasBody, reply.Body is not null);
        Assert.Same(failure, Assert.Single(reported));
    }

    [Fact]
    public async Task Dispatch_FailingErrorObserverStillReturns500()
    {
        Harness harness = new()
        {
            Dispatch = _ => Task.FromException<SocketDispatchResult>(new InvalidOperationException("handler failed")),
            OnError = _ => throw new InvalidOperationException("observer failed"),
        };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope("invoke", """{"type":"invoke"}"""));

        Assert.Equal(500, reply?.Status);
    }

    [Fact]
    public async Task Dispatch_RejectsUnsupportedProtocolVersionWithoutDispatching()
    {
        Harness harness = new();
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(
            Envelope("message", """{"type":"message"}""", protocolVersion: SocketModeProtocol.CurrentVersion + 1));

        Assert.Equal(400, reply?.Status);
        Assert.Empty(harness.Dispatched);
    }

    [Fact]
    public async Task Dispatch_DropsEnvelopeWithoutActivity()
    {
        Harness harness = new();
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope("message", """{"text":"no type"}"""));

        Assert.Null(reply);
        Assert.Empty(harness.Dispatched);
    }

    [Fact]
    public void RetryPolicy_UsesScheduleThenRepeatsLastDelay()
    {
        IGeoSocketOwner owner = new Harness(new SocketModeTransportOptions
        {
            ReconnectDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)],
        }).Transport;

        Assert.Equal(TimeSpan.FromSeconds(1), owner.GetBackoffDelay(0));
        Assert.Equal(TimeSpan.FromSeconds(4), owner.GetBackoffDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(4), owner.GetBackoffDelay(9));
    }

    [Fact]
    public void RetryPolicy_JittersCappedExponentialBackoff()
    {
        IGeoSocketOwner owner = new Harness { Random = new FixedRandom(0.5) }.Transport;

        Assert.Equal(TimeSpan.FromSeconds(0.5), owner.GetBackoffDelay(0));
        Assert.Equal(TimeSpan.FromSeconds(2), owner.GetBackoffDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(7.5), owner.GetBackoffDelay(100));
    }

    [Fact]
    public void RetryPolicy_ReadsRetryAfterFromNegotiateFailure()
    {
        IGeoSocketOwner owner = new Harness().Transport;

        Assert.Equal(
            TimeSpan.FromSeconds(7),
            owner.GetRetryAfter(new SocketModeNegotiateException(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(7))));
        Assert.Null(owner.GetRetryAfter(new IOException()));
        Assert.Null(owner.GetRetryAfter(null));
    }

    private static SocketActivityEnvelope Envelope(string? type, string payload, int protocolVersion = 1)
        => new()
        {
            ProtocolVersion = protocolVersion,
            EnvelopeId = "env-1",
            Type = type,
            Payload = JsonDocument.Parse(payload).RootElement.Clone(),
        };

    private static string[] SortedUris(FakeConnection[] connections)
        => [.. connections.Select(connection => connection.NegotiateUri.AbsoluteUri).Order(StringComparer.Ordinal)];

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    private sealed class Harness
    {
        private readonly SocketModeTransportOptions _options;
        private SocketModeTransport? _transport;

        internal Harness(SocketModeTransportOptions? options = null)
        {
            _options = options ?? new SocketModeTransportOptions();
        }

        internal FakeConnectionFactory Factory { get; } = new();

        internal List<CoreActivity> Dispatched { get; } = [];

        internal Func<CoreActivity, Task<SocketDispatchResult>> Dispatch { get; init; } =
            _ => Task.FromResult(new SocketDispatchResult(200));

        internal Func<Exception, Task>? OnError { get; init; }

        internal Random? Random { get; init; }

        internal SocketModeTransport Transport => _transport ??= new SocketModeTransport(
            _options,
            Factory,
            activity =>
            {
                lock (Dispatched)
                {
                    Dispatched.Add(activity);
                }

                return Dispatch(activity);
            },
            NullLogger.Instance,
            "bot-id",
            OnError,
            new FixedTimeProvider(),
            Random);

        internal async Task<FakeConnection[]> StartReadyAsync()
        {
            Task start = Transport.StartAsync();
            FakeConnection[] connections = await Factory.WaitForAsync(_options.Geos.Count);
            foreach (FakeConnection connection in connections)
            {
                connection.Ready();
            }

            await start;
            return connections;
        }
    }

    private sealed class FakeConnectionFactory : ISocketConnectionFactory
    {
        private readonly Dictionary<string, Exception> _startFailures = [];
        private readonly List<FakeConnection> _connections = [];

        internal FakeConnection[] Connections
        {
            get
            {
                lock (_connections)
                {
                    return [.. _connections];
                }
            }
        }

        internal void FailStart(string geo, Exception error) => _startFailures[geo] = error;

        internal async Task<FakeConnection[]> WaitForAsync(int count)
        {
            await WaitUntilAsync(() => Connections.Length >= count);
            FakeConnection[] connections = Connections;
            await Task.WhenAll(connections.Select(connection => connection.Started.Task));
            return connections;
        }

        public ISocketConnection Create(Uri negotiateUri, SocketConnectionHandlers handlers)
        {
            string[] segments = negotiateUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string geo = segments.Length > 3 ? segments[0] : string.Empty;
            FakeConnection connection = new(negotiateUri, geo, handlers, _startFailures.GetValueOrDefault(geo));
            lock (_connections)
            {
                _connections.Add(connection);
            }

            return connection;
        }
    }

    private sealed class FakeConnection(
        Uri negotiateUri,
        string geo,
        SocketConnectionHandlers handlers,
        Exception? startError) : ISocketConnection
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _closed;
        private int _stopCount;
        private int _disposeCount;

        public TimeSpan? TokenLifetime => null;

        internal Uri NegotiateUri { get; } = negotiateUri;

        internal string Geo { get; } = geo;

        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal int StopCount => Volatile.Read(ref _stopCount);

        internal int DisposeCount => Volatile.Read(ref _disposeCount);

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
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }

        internal void Ready()
        {
            handlers.OnReady(new SocketReadyFrame { ConnectionId = NegotiateUri.AbsolutePath });
            _ready.TrySetResult();
        }

        internal void Close(Exception error) => RaiseClosed(error, planned: false);

        internal Task<SocketReplyFrame?> ActivityAsync(SocketActivityEnvelope envelope) => handlers.OnActivity(envelope);

        private void RaiseClosed(Exception? error, bool planned)
        {
            if (Interlocked.Exchange(ref _closed, 1) == 0)
            {
                handlers.OnClosed(error, planned);
            }
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
