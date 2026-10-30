// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

/// <summary>
/// Runs Socket Mode end to end in process: transport, geo supervisors, negotiator and SignalR connection factory are real; only HTTP and the SignalR client are faked.
/// </summary>
public class SocketModeEndToEndTests
{
    [Fact]
    public async Task Transport_ComposesNegotiatorAndSignalRFactoryAcrossGeos()
    {
        EndToEnd stack = new();

        Task start = stack.Transport.StartAsync();
        FakeSignalRClient[] clients = await stack.SignalR.WaitForAsync(3);
        foreach (FakeSignalRClient client in clients)
        {
            client.Ready();
        }

        await start;

        Assert.Equal(SocketModeStatus.Ready, stack.Transport.Status);
        Assert.Equal(
            [
                "https://botapi.skype.com/amer/v3/websockets/connect",
                "https://botapi.skype.com/apac/v3/websockets/connect",
                "https://botapi.skype.com/emea/v3/websockets/connect",
            ],
            stack.Http.Requests.Select(request => request.AbsoluteUri).Order(StringComparer.Ordinal));
        Assert.All(stack.Http.Authorizations, authorization => Assert.Equal("Bearer bot-token", authorization));
        Assert.Equal(
            ["https://signalr.test/amer", "https://signalr.test/apac", "https://signalr.test/emea"],
            clients.Select(client => client.Url.AbsoluteUri).Order(StringComparer.Ordinal));

        FakeSignalRClient emea = clients.Single(client => client.Url.AbsolutePath == "/emea");
        SocketReplyFrame? reply = await emea.ActivityAsync(new SocketActivityEnvelope
        {
            ProtocolVersion = SocketModeProtocol.CurrentVersion,
            EnvelopeId = "env-1",
            Type = "invoke",
            Payload = JsonDocument.Parse("""{"type":"invoke","id":"a1"}""").RootElement.Clone(),
        });

        Assert.NotNull(reply);
        Assert.Equal("env-1", reply.EnvelopeId);
        Assert.Equal("bot-id", reply.BotKey);
        Assert.Equal(200, reply.Status);
        Assert.Equal("a1", Assert.Single(stack.Dispatched).Id);

        await stack.Transport.StopAsync();

        Assert.Equal(SocketModeStatus.Stopped, stack.Transport.Status);
        Assert.All(clients, client =>
        {
            Assert.Equal(1, client.StopCount);
            Assert.Equal(1, client.DisposeCount);
        });
    }

    [Fact]
    public async Task Transport_HonorsNegotiatorRetryAfterDuringStartup()
    {
        EndToEnd stack = new(new SocketModeTransportOptions { Geos = ["amer"] });
        stack.Http.ThrottleOnce(TimeSpan.Zero);

        Task start = stack.Transport.StartAsync();
        FakeSignalRClient client = (await stack.SignalR.WaitForAsync(1))[0];
        client.Ready();
        await start;

        Assert.Equal(2, stack.Http.Requests.Count);
        Assert.Equal(SocketModeStatus.Ready, stack.Transport.Status);

        await stack.Transport.DisposeAsync();
    }

    [Fact]
    public async Task Transport_FailsStartupWithNegotiatorErrorWhenBudgetIsExhausted()
    {
        EndToEnd stack = new(new SocketModeTransportOptions { Geos = ["amer"], StartupTimeout = TimeSpan.Zero });
        stack.Http.ThrottleOnce(TimeSpan.Zero);

        SocketModeNegotiateException error =
            await Assert.ThrowsAsync<SocketModeNegotiateException>(() => stack.Transport.StartAsync());

        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
        Assert.Equal(SocketModeStatus.Stopped, stack.Transport.Status);
        Assert.Empty(stack.SignalR.Clients);
    }

    private sealed class EndToEnd
    {
        internal EndToEnd(SocketModeTransportOptions? options = null)
        {
            SocketModeNegotiator negotiator = new(
                new HttpClient(Http),
                _ => Task.FromResult<string?>("bot-token"));
            SignalRSocketConnectionFactory factory = new(
                negotiator,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30),
                NullLogger.Instance,
                SignalR.Create);
            Transport = new SocketModeTransport(
                options ?? new SocketModeTransportOptions(),
                factory,
                activity =>
                {
                    Dispatched.Enqueue(activity);
                    return Task.FromResult(new SocketDispatchResult(200, null));
                },
                NullLogger.Instance,
                botKey: "bot-id");
        }

        internal NegotiateHandler Http { get; } = new();
        internal FakeSignalRClientFactory SignalR { get; } = new();
        internal ConcurrentQueue<CoreActivity> Dispatched { get; } = new();
        internal SocketModeTransport Transport { get; }
    }

    private sealed class NegotiateHandler : HttpMessageHandler
    {
        private readonly object _gate = new();
        private TimeSpan? _throttle;

        internal ConcurrentQueue<Uri> Requests { get; } = new();
        internal ConcurrentQueue<string?> Authorizations { get; } = new();

        internal void ThrottleOnce(TimeSpan retryAfter)
        {
            lock (_gate)
            {
                _throttle = retryAfter;
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Uri uri = request.RequestUri ?? throw new InvalidOperationException("Missing request URI.");
            Requests.Enqueue(uri);
            Authorizations.Enqueue(request.Headers.Authorization?.ToString());

            TimeSpan? throttle;
            lock (_gate)
            {
                throttle = _throttle;
                _throttle = null;
            }

            if (throttle is TimeSpan retryAfter)
            {
                HttpResponseMessage throttled = new(HttpStatusCode.TooManyRequests);
                throttled.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter);
                return Task.FromResult(throttled);
            }

            string geo = uri.Segments[1].TrimEnd('/');
            string json = $$"""{"url":"https://signalr.test/{{geo}}","accessToken":"token-{{geo}}","expiresIn":3600}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FakeSignalRClientFactory
    {
        private readonly object _gate = new();
        private TaskCompletionSource _changed = NewSignal();

        internal List<FakeSignalRClient> Clients { get; } = [];

        internal ISignalRClientConnection Create(
            Uri url,
            string accessToken,
            TimeSpan keepAliveInterval,
            TimeSpan serverTimeout)
        {
            FakeSignalRClient client = new(url, Changed);
            lock (_gate)
            {
                Clients.Add(client);
            }

            Changed();
            return client;
        }

        internal async Task<FakeSignalRClient[]> WaitForAsync(int count)
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
            while (true)
            {
                Task changed;
                lock (_gate)
                {
                    if (Clients.Count >= count && Clients.Take(count).All(client => client.StartCount > 0))
                    {
                        return [.. Clients.Take(count)];
                    }

                    changed = _changed.Task;
                }

                await changed.WaitAsync(timeout.Token);
            }
        }

        private void Changed()
        {
            TaskCompletionSource previous;
            lock (_gate)
            {
                previous = _changed;
                _changed = NewSignal();
            }

            previous.TrySetResult();
        }

        private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class FakeSignalRClient(Uri url, Action changed) : ISignalRClientConnection
    {
        private Func<SocketActivityEnvelope, Task<SocketReplyFrame?>>? _onActivity;
        private Action<SocketReadyFrame>? _onReady;
        private Action<Exception?>? _onClosed;
        private int _startCount;
        private int _stopCount;
        private int _disposeCount;

        internal Uri Url { get; } = url;
        internal int StartCount => Volatile.Read(ref _startCount);
        internal int StopCount => Volatile.Read(ref _stopCount);
        internal int DisposeCount => Volatile.Read(ref _disposeCount);

        public void OnActivity(Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> handler) => _onActivity = handler;

        public void OnReady(Action<SocketReadyFrame> handler) => _onReady = handler;

        public void OnClosed(Action<Exception?> handler) => _onClosed = handler;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _startCount);
            changed();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _stopCount);
            _onClosed?.Invoke(null);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }

        internal void Ready()
            => (_onReady ?? throw new InvalidOperationException())(new SocketReadyFrame { BotKey = "bot-id" });

        internal Task<SocketReplyFrame?> ActivityAsync(SocketActivityEnvelope envelope)
            => (_onActivity ?? throw new InvalidOperationException())(envelope);
    }
}
