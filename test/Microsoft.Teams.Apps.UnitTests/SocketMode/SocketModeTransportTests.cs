// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeTransportTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_ConnectsEveryDefaultGeo(bool emptyGeos)
    {
        Harness harness = new(emptyGeos ? new SocketModeTransportOptions { Geos = [] } : null);

        Task start = harness.Transport.StartAsync();
        FakeConnection[] connections = await harness.Factory.WaitForAsync(3);

        Assert.Equal(
            [
                "https://amer.botapi.skype.com/v3/websockets/connect",
                "https://apac.botapi.skype.com/v3/websockets/connect",
                "https://emea.botapi.skype.com/v3/websockets/connect",
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
        await harness.Transport.DisposeAsync();
    }

    [Theory]
    [InlineData(new object[] { new[] { "emea" } })]
    [InlineData(new object[] { new[] { "custom-geo", "other-geo" } })]
    [InlineData(new object[] { new[] { "amer", "AMER" } })]
    [InlineData(new object[] { new[] { "amer.amer" } })]
    public async Task StartAsync_ConnectsConfiguredGeos(string[] geos)
    {
        Harness harness = new(new SocketModeTransportOptions
        {
            Geos = geos,
        });

        FakeConnection[] connections = await harness.StartReadyAsync();

        Assert.Equal(
            geos.Select(geo => $"https://{geo.ToLowerInvariant()}.botapi.skype.com/v3/websockets/connect").Order(StringComparer.Ordinal),
            SortedUris(connections));
        Assert.Equal(geos, harness.Transport.GeoList);
        Assert.Equal(geos.Length, harness.Transport.GeoStatuses.Count);
        Assert.All(harness.Transport.GeoStatuses.Values, status => Assert.Equal(SocketModeStatus.Ready, status));
        await harness.Transport.DisposeAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new object[] { new string[] { null! } })]
    [InlineData(new object[] { new[] { "" } })]
    [InlineData(new object[] { new[] { "amer", "" } })]
    [InlineData(new object[] { new[] { "amer", "amer" } })]
    public void Constructor_RejectsInvalidGeos(string[]? geos)
    {
        Assert.Throws<ArgumentException>(() => new Harness(new SocketModeTransportOptions { Geos = geos! }).Transport);
    }

    [Fact]
    public void Constructor_DoesNotInterpretGeoAsUrlSyntax()
    {
        Assert.Throws<UriFormatException>(() =>
            new Harness(new SocketModeTransportOptions { Geos = ["attacker.example#"] }).Transport);
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
        ConcurrentQueue<SocketModeDisconnectedEventArgs> disconnected = new();
        harness.Events.Disconnected += (_, e) => disconnected.Enqueue(e);
        harness.Factory.FailStart("emea", failure);

        IOException thrown = await Assert.ThrowsAsync<IOException>(() => harness.Transport.StartAsync());

        Assert.Same(failure, thrown);
        Assert.Equal(SocketModeStatus.Stopped, harness.Transport.Status);
        Assert.All(harness.Factory.Connections, connection => Assert.Equal(1, connection.DisposeCount));
        Assert.Empty(disconnected);
    }

    [Fact]
    public async Task StartAsync_WhenCleanupFails_StillThrowsTheStartupFailure()
    {
        IOException failure = new("emea failed");
        Harness harness = new(new SocketModeTransportOptions { StartupTimeout = TimeSpan.Zero });
        harness.Factory.FailDispose("amer", new IOException("amer dispose failed"));
        harness.Factory.FailDispose("apac", new IOException("apac dispose failed"));

        Task start = harness.Transport.StartAsync();
        FakeConnection[] connections = await harness.Factory.WaitForAsync(3);
        connections.Single(connection => connection.Geo == "amer").Ready();
        connections.Single(connection => connection.Geo == "apac").Ready();
        connections.Single(connection => connection.Geo == "emea").Fail(failure);

        IOException thrown = await Assert.ThrowsAsync<IOException>(() => start);

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
        ConcurrentQueue<SocketModeDisconnectedEventArgs> disconnected = new();
        harness.Events.Disconnected += (_, e) => disconnected.Enqueue(e);
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
        Assert.Empty(disconnected);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LifecycleEvents_RecoveryWaitsForReadyObserversWithoutBlockingDeliveryOrShutdown(bool stopWhileObserving)
    {
        Harness harness = new(new SocketModeTransportOptions
        {
            Geos = ["amer"],
            ReconnectDelays = [TimeSpan.Zero],
        });
        ConcurrentQueue<string> notifications = new();
        ConcurrentQueue<SocketModeReadyEventArgs> ready = new();
        ConcurrentQueue<SocketModeDisconnectedEventArgs> disconnected = new();
        TaskCompletionSource observing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource reconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseObserver = new();
        int readyFinished = 0;
        int readyFinishedAtReconnect = -1;
        harness.Events.Ready += (sender, e) =>
        {
            Assert.Same(harness.Events, sender);
            Assert.Equal(SocketModeStatus.Ready, harness.Transport.GeoStatuses[e.Geo]);
            ready.Enqueue(e);
            if (ready.Count > 1)
            {
                observing.TrySetResult();
                if (!releaseObserver.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("Test did not release the Ready observer.");
                }

                Volatile.Write(ref readyFinished, 1);
            }

            notifications.Enqueue("ready");
        };
        harness.Events.Disconnected += (_, e) =>
        {
            Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.GeoStatuses[e.Geo]);
            disconnected.Enqueue(e);
            notifications.Enqueue("disconnected");
        };
        harness.Events.Reconnected += (_, e) =>
        {
            Assert.Equal("amer", e.Geo);
            Assert.Equal(SocketModeStatus.Ready, harness.Transport.Status);
            readyFinishedAtReconnect = Volatile.Read(ref readyFinished);
            notifications.Enqueue("reconnected");
            reconnected.TrySetResult();
        };

        FakeConnection initial = Assert.Single(await harness.StartReadyAsync());
        await WaitUntilAsync(() => notifications.Count == 1);
        SocketModeReadyEventArgs firstReady = Assert.Single(ready);
        Assert.Equal("amer", firstReady.Geo);
        Assert.Equal(initial.NegotiateUri.AbsolutePath, firstReady.Frame.ConnectionId);

        IOException dropped = new("dropped");
        initial.Close(dropped);
        SocketModeDisconnectedEventArgs interruption = Assert.Single(disconnected);
        Assert.Equal("amer", interruption.Geo);
        Assert.Same(dropped, interruption.Error);
        Assert.False(interruption.Terminal);

        FakeConnection replacement = (await harness.Factory.WaitForAsync(2))[1];
        replacement.CompleteReadiness();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (await replacement.ActivityAsync(Envelope("activity", """{"type":"message","id":"before-ready"}""")) is null)
        {
            await Task.Delay(1, timeout.Token);
        }

        await Assert.ThrowsAsync<TimeoutException>(() => reconnected.Task.WaitAsync(TimeSpan.FromMilliseconds(100)));

        Task notify = Task.Run(replacement.NotifyReady);
        try
        {
            await observing.Task.WaitAsync(TimeSpan.FromSeconds(5));
            SocketReplyFrame? reply = await replacement.ActivityAsync(Envelope("activity", """{"type":"message","id":"during-ready"}"""));
            Assert.NotNull(reply);
            Assert.Equal(["before-ready", "during-ready"], harness.Dispatched.Select(activity => activity.Id));
            await Assert.ThrowsAsync<TimeoutException>(() => reconnected.Task.WaitAsync(TimeSpan.FromMilliseconds(100)));

            if (stopWhileObserving)
            {
                await harness.Transport.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Assert.False(reconnected.Task.IsCompleted);
            }
        }
        finally
        {
            releaseObserver.Set();
            await notify.WaitAsync(TimeSpan.FromSeconds(5));
            if (!stopWhileObserving)
            {
                await reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(1, readyFinishedAtReconnect);
            }

            await harness.Transport.StopAsync();
        }

        Assert.Equal(2, ready.Count);
        Assert.Equal("amer", ready.Last().Geo);
        Assert.Equal(replacement.NegotiateUri.AbsolutePath, ready.Last().Frame.ConnectionId);
        string[] expected = stopWhileObserving
            ? ["ready", "disconnected", "ready"]
            : ["ready", "disconnected", "ready", "reconnected"];
        Assert.Equal(expected, notifications.ToArray());
    }

    [Theory]
    [InlineData("before-ready", false)]
    [InlineData("during-ready", false)]
    [InlineData("inside-ready", false)]
    [InlineData("during-ready", true)]
    public async Task LifecycleEvents_ClosedReplacementCannotReportRecovery(string closeTiming, bool terminalNextAttempt)
    {
        Harness harness = new(new SocketModeTransportOptions
        {
            Geos = ["amer"],
            ReconnectDelays = [TimeSpan.Zero],
        });
        FakeConnection initial = Assert.Single(await harness.StartReadyAsync());
        initial.Close(new IOException("initial drop"));
        FakeConnection replacement = (await harness.Factory.WaitForAsync(2))[1];
        using ManualResetEventSlim releaseReady = new();
        TaskCompletionSource observing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<SocketModeDisconnectedEventArgs> disconnected = new();
        int readyCount = 0;
        int reconnections = 0;
        harness.Events.Disconnected += (_, e) => disconnected.Enqueue(e);
        harness.Events.Reconnected += (_, _) => Interlocked.Increment(ref reconnections);
        IOException dropped = new("replacement dropped during readiness");
        harness.Events.Ready += (_, _) =>
        {
            if (Interlocked.Increment(ref readyCount) != 1)
            {
                return;
            }

            observing.TrySetResult();
            if (closeTiming == "during-ready")
            {
                if (!releaseReady.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("Test did not release the Ready observer.");
                }

                throw new InvalidOperationException("Observer failure must not prevent recovery.");
            }
            if (closeTiming == "inside-ready")
            {
                replacement.Close(dropped);
            }
        };
        if (terminalNextAttempt)
        {
            harness.Factory.FailStart("amer", new SocketModeNegotiateException(HttpStatusCode.Forbidden, retryAfter: null));
        }

        replacement.CompleteReadiness();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (await replacement.ActivityAsync(Envelope("activity", """{"type":"message","id":"before-close"}""")) is null)
        {
            await Task.Delay(1, timeout.Token);
        }

        Task notify = Task.CompletedTask;
        try
        {
            if (closeTiming != "before-ready")
            {
                notify = Task.Run(replacement.NotifyReady);
                await observing.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            if (closeTiming != "inside-ready")
            {
                replacement.Close(dropped);
            }

            FakeConnection next = (await harness.Factory.WaitForAsync(3))[2];
            Assert.Equal(0, Volatile.Read(ref reconnections));
            Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.Status);
            Assert.Same(dropped, disconnected.First().Error);
            Assert.Null(await replacement.ActivityAsync(Envelope("activity", """{"type":"message","id":"after-close"}""")));

            releaseReady.Set();
            await notify.WaitAsync(TimeSpan.FromSeconds(5));
            if (closeTiming == "before-ready")
            {
                replacement.NotifyReady();
            }
            Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.Status);
            Assert.Equal(0, Volatile.Read(ref reconnections));

            if (terminalNextAttempt)
            {
                await WaitUntilAsync(() => disconnected.Any(e => e.Terminal));
                Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.Status);
            }
            else
            {
                next.Ready();
                await WaitUntilAsync(() => Volatile.Read(ref reconnections) == 1);
                Assert.Equal(SocketModeStatus.Ready, harness.Transport.Status);
            }
        }
        finally
        {
            releaseReady.Set();
            await notify.WaitAsync(TimeSpan.FromSeconds(5));
            await harness.Transport.StopAsync();
        }

        Assert.Equal(terminalNextAttempt ? 0 : 1, reconnections);
    }

    [Theory]
    [InlineData("before-observer", false)]
    [InlineData("during-observer", false)]
    [InlineData("inside-observer", false)]
    [InlineData("during-observer", true)]
    public async Task LifecycleEvents_CloseDuringRecoveryNotificationIsReportedAfterRecovery(string closeTiming, bool stopAfterClose)
    {
        ErrorLogger logger = new();
        Harness harness = new(new SocketModeTransportOptions
        {
            Geos = ["amer"],
            ReconnectDelays = [TimeSpan.Zero],
        }) { Logger = logger };
        FakeConnection initial = Assert.Single(await harness.StartReadyAsync());
        initial.Close(new IOException("initial drop"));
        FakeConnection replacement = (await harness.Factory.WaitForAsync(2))[1];
        TaskCompletionSource observing = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseRecovery = new();
        ConcurrentQueue<string> notifications = new();
        int reconnections = 0;
        int recoveryLogs = 0;
        void WaitForRelease()
        {
            observing.TrySetResult();
            if (!releaseRecovery.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Test did not release recovery reporting.");
            }
        }

        logger.InformationLogged = message =>
        {
            if (closeTiming == "before-observer"
                && message.Contains("inbound delivery resumed", StringComparison.Ordinal)
                && Interlocked.Increment(ref recoveryLogs) == 1)
            {
                WaitForRelease();
            }
        };
        harness.Events.Disconnected += (_, _) => notifications.Enqueue("disconnected");
        harness.Events.Reconnected += (_, _) =>
        {
            if (Interlocked.Increment(ref reconnections) == 1)
            {
                if (closeTiming == "inside-observer")
                {
                    observing.TrySetResult();
                    replacement.Close(new IOException("closed by observer"));
                }
                else if (closeTiming == "during-observer")
                {
                    WaitForRelease();
                }
            }
            notifications.Enqueue("reconnected");
        };

        replacement.Ready();
        try
        {
            await observing.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (closeTiming != "inside-observer")
            {
                Assert.NotNull(await replacement.ActivityAsync(Envelope("activity", """{"type":"message","id":"during-reconnected"}""")));
                await Task.Run(() => replacement.Close(new IOException("concurrent drop"))).WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Null(await replacement.ActivityAsync(Envelope("activity", """{"type":"message","id":"after-close"}""")));
                Assert.Empty(notifications);
            }

            if (stopAfterClose)
            {
                Task stop = harness.Transport.StopAsync();
                Assert.Equal(SocketModeStatus.Stopped, harness.Transport.Status);
                releaseRecovery.Set();
                await stop.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.Equal(["reconnected", "disconnected"], notifications.ToArray());
                Assert.Equal(SocketModeStatus.Stopped, harness.Transport.Status);
                Assert.Equal(1, reconnections);
                return;
            }

            releaseRecovery.Set();
            FakeConnection next = (await harness.Factory.WaitForAsync(3))[2];
            Assert.Equal(["reconnected", "disconnected"], notifications.ToArray());
            Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.Status);
            next.Ready();
            await WaitUntilAsync(() => notifications.Count == 3);
            Assert.Equal(["reconnected", "disconnected", "reconnected"], notifications.ToArray());
            Assert.Equal(SocketModeStatus.Ready, harness.Transport.Status);
        }
        finally
        {
            releaseRecovery.Set();
            await harness.Transport.StopAsync();
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task LifecycleEvents_ReportTerminalRejectionAfterNetworkDrop(HttpStatusCode status)
    {
        ErrorLogger logger = new();
        Harness harness = new(new SocketModeTransportOptions
        {
            Geos = ["amer", "emea"],
            ReconnectDelays = [TimeSpan.Zero],
        }) { Logger = logger };
        ConcurrentQueue<SocketModeDisconnectedEventArgs> disconnected = new();
        int reconnections = 0;
        harness.Events.Disconnected += (_, e) => disconnected.Enqueue(e);
        harness.Events.Reconnected += (_, _) => Interlocked.Increment(ref reconnections);
        FakeConnection[] initial = await harness.StartReadyAsync();
        SocketModeNegotiateException rejected = new(status, retryAfter: null);
        harness.Factory.FailStart("amer", rejected);

        initial.Single(connection => connection.Geo == "amer").Close(new IOException("dropped"));
        await WaitUntilAsync(() => disconnected.Count == 2);

        SocketModeDisconnectedEventArgs[] events = disconnected.ToArray();
        Assert.False(events[0].Terminal);
        Assert.True(events[1].Terminal);
        Assert.Equal("amer", events[1].Geo);
        Assert.Same(rejected, events[1].Error);
        Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.GeoStatuses["amer"]);
        Assert.Equal(SocketModeStatus.Ready, harness.Transport.GeoStatuses["emea"]);
        Assert.Equal(SocketModeStatus.Disconnected, harness.Transport.Status);
        Assert.Equal(0, reconnections);
        var error = Assert.Single(logger.Records, record => record.Level == LogLevel.Error);
        Assert.Contains("stopped until the app is restarted", error.Message, StringComparison.Ordinal);
        Assert.Same(rejected, error.Exception);
        await harness.Transport.StopAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lifecycle_LogsAggregateAfterEachGeoChangeAndStopsOnce(bool subscribe)
    {
        ErrorLogger logger = new();
        Harness harness = new(new SocketModeTransportOptions
        {
            Geos = ["amer", "emea"],
            ReconnectDelays = [TimeSpan.Zero],
        }) { Logger = logger };
        if (subscribe)
        {
            harness.Events.Ready += (_, _) => { };
            harness.Events.Disconnected += (_, _) => { };
            harness.Events.Reconnected += (_, _) => { };
        }

        Task start = harness.Transport.StartAsync();
        FakeConnection[] connections = await harness.Factory.WaitForAsync(2);
        connections[0].Ready();
        connections[1].Ready();
        await start;

        IOException dropped = new("dropped");
        connections[0].Close(dropped);
        FakeConnection replacement = (await harness.Factory.WaitForAsync(3))[2];
        replacement.Ready();
        await WaitUntilAsync(() => logger.Information.Any(message => message.Contains("inbound delivery resumed", StringComparison.Ordinal)));
        await harness.Transport.StopAsync();
        await harness.Transport.StopAsync();

        Assert.Equal(
            [
                "Socket Mode aggregate status: Connecting.",
                "Socket Mode aggregate status: Connecting.",
                "Socket Mode aggregate status: Ready.",
                "Socket Mode aggregate status: Disconnected.",
                "Socket Mode aggregate status: Ready.",
                "Socket Mode aggregate status: Stopped.",
            ],
            logger.Information.Where(message => message.StartsWith("Socket Mode aggregate status:", StringComparison.Ordinal)));
        Assert.Contains(
            $"Socket Mode geo {connections[0].Geo} ready with connection {connections[0].NegotiateUri.AbsolutePath}.",
            logger.Information);
        var warning = Assert.Single(logger.Records, record => record.Level == LogLevel.Warning);
        Assert.Contains("inbound delivery paused", warning.Message, StringComparison.Ordinal);
        Assert.Same(dropped, warning.Exception);
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
    public async Task Dispatch_LogsFailuresThePipelineHasNotAlreadyLogged()
    {
        ErrorLogger logger = new();
        InvalidOperationException failure = new("dispatch failed");
        Harness harness = new() { Logger = logger, Dispatch = _ => throw failure };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        await connection.ActivityAsync(Envelope("message", """{"type":"message"}"""));

        Assert.Same(failure, Assert.Single(logger.Errors));
    }

    [Fact]
    public async Task Dispatch_DoesNotLogHandlerFailuresTheBotAlreadyLogged()
    {
        ErrorLogger logger = new();
        Harness harness = new()
        {
            Logger = logger,
            Dispatch = activity => throw new BotHandlerException("handler failed", new InvalidOperationException(), activity),
        };
        FakeConnection connection = (await harness.StartReadyAsync())[0];

        SocketReplyFrame? reply = await connection.ActivityAsync(Envelope("invoke", """{"type":"invoke"}"""));

        Assert.Equal(500, reply?.Status);
        Assert.Empty(logger.Errors);
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

    private sealed class ErrorLogger : ILogger
    {
        private readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> _records = new();

        internal Action<string>? InformationLogged { get; set; }

        internal (LogLevel Level, string Message, Exception? Exception)[] Records => _records.ToArray();

        internal string[] Information => [.. _records.Where(record => record.Level == LogLevel.Information).Select(record => record.Message)];

        internal Exception?[] Errors => [.. _records.Where(record => record.Level == LogLevel.Error).Select(record => record.Exception)];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            _records.Enqueue((logLevel, message, exception));
            if (logLevel == LogLevel.Information)
            {
                InformationLogged?.Invoke(message);
            }
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

        internal ILogger Logger { get; init; } = NullLogger.Instance;

        internal SocketModeEvents Events { get; } = new(NullLogger<SocketModeEvents>.Instance);

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
            Logger,
            "bot-id",
            OnError,
            new FixedTimeProvider(),
            Random,
            Events);

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
        private readonly Dictionary<string, Exception> _disposeFailures = [];
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

        internal void FailDispose(string geo, Exception error) => _disposeFailures[geo] = error;

        internal async Task<FakeConnection[]> WaitForAsync(int count)
        {
            await WaitUntilAsync(() => Connections.Length >= count);
            FakeConnection[] connections = Connections;
            await Task.WhenAll(connections.Select(connection => connection.Started.Task));
            return connections;
        }

        public ISocketConnection Create(Uri negotiateUri, SocketConnectionHandlers handlers, Func<string?>? getReplacedConnectionId = null)
        {
            string geo = negotiateUri.Host.Split('.')[0];
            FakeConnection connection = new(
                negotiateUri,
                geo,
                handlers,
                _startFailures.GetValueOrDefault(geo),
                _disposeFailures.GetValueOrDefault(geo));
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
        Exception? startError,
        Exception? disposeError) : ISocketConnection
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
            return disposeError is null ? ValueTask.CompletedTask : ValueTask.FromException(disposeError);
        }

        internal void Ready()
        {
            CompleteReadiness();
            NotifyReady();
        }

        internal void CompleteReadiness() => _ready.TrySetResult();

        internal void NotifyReady() => handlers.OnReady(new SocketReadyFrame { ConnectionId = NegotiateUri.AbsolutePath });

        internal void Fail(Exception error) => _ready.TrySetException(error);

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
