// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core.Diagnostics;
using Microsoft.Teams.Core.Schema;
using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Core.UnitTests.Diagnostics;

public class TelemetryTests
{
    [Fact]
    public void CoreTelemetryNames_ConstantsHaveExpectedValues()
    {
        Assert.Equal("Microsoft.Teams.Core", CoreTelemetryNames.ActivitySourceName);
        Assert.Equal("Microsoft.Teams.Core", CoreTelemetryNames.MeterName);
    }

    [Fact]
    public async Task ProcessAsync_EmitsTurnSpanWithExpectedTags()
    {
        using SpanCapture capture = new();

        BotApplication botApp = CreateBotApplication();
        botApp.OnActivity = (_, _) => Task.CompletedTask;

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act-1",
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://smba.example/"),
            Conversation = new("conv-1"),
        };

        await botApp.ProcessAsync(BuildHttpContext(activity));

        Activity turn = Assert.Single(capture.Stopped, a => a.OperationName == "turn");
        Assert.Equal("act-1", turn.GetTagItem("activity.id"));
        Assert.Equal("msteams", turn.GetTagItem("channel.id"));
        Assert.Equal("conv-1", turn.GetTagItem("conversation.id"));
        Assert.Equal("https://smba.example/", turn.GetTagItem("service.url"));
        Assert.Equal(ActivityStatusCode.Unset, turn.Status);
    }

    [Fact]
    public async Task ProcessAsync_NestsMiddlewareSpansUnderTurn()
    {
        using SpanCapture capture = new();

        BotApplication botApp = CreateBotApplication();
        Mock<ITurnMiddleware> mw = new();
        mw.Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
          .Returns<BotApplication, CoreActivity, NextTurn, CancellationToken>((_, _, next, ct) => next(ct));
        botApp.UseMiddleware(mw.Object);
        botApp.OnActivity = (_, _) => Task.CompletedTask;

        await botApp.ProcessAsync(BuildHttpContext(NewActivity()));

        Activity turn = Assert.Single(capture.Stopped, a => a.OperationName == "turn");
        Activity middleware = Assert.Single(capture.Stopped, a => a.OperationName == "middleware");
        Assert.Equal(turn.SpanId, middleware.ParentSpanId);
        Assert.Equal(0, middleware.GetTagItem("middleware.index"));
        Assert.NotNull(middleware.GetTagItem("middleware.name"));
    }

    [Fact]
    public async Task ProcessAsync_RecordsExceptionOnTurnSpanAndIncrementsErrorCounter()
    {
        using SpanCapture spanCapture = new();
        using MetricCapture metricCapture = new();

        BotApplication botApp = CreateBotApplication();
        botApp.OnActivity = (_, _) => throw new InvalidOperationException("boom");

        await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(BuildHttpContext(NewActivity())));

        Activity turn = Assert.Single(spanCapture.Stopped, a => a.OperationName == "turn");
        Assert.Equal(ActivityStatusCode.Error, turn.Status);
        Assert.Contains(turn.Events, e => e.Name == "exception");

        Assert.True(metricCapture.GetCounterTotal("teams.handler.errors") >= 1);
        Assert.True(metricCapture.GetCounterTotal("teams.activities.received") >= 1);
        Assert.True(metricCapture.HistogramSampleCount("teams.turn.duration") >= 1);
    }

    [Fact]
    public async Task ConversationClient_SendActivityAsync_EmitsConversationClientSpanAndOutboundCallsCounter()
    {
        using SpanCapture spanCapture = new();
        using MetricCapture metricCapture = new();

        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"sent-1\"}"),
            });

        ConversationClient client = new(new HttpClient(handler.Object));

        SendActivityResponse? response = await client.SendActivityAsync("conv-1", new CoreActivity
        {
            Type = ActivityType.Message,
            ServiceUrl = new Uri("https://smba.example/"),
            Conversation = new("conv-1"),
        }, new Uri("https://smba.example/"));

        Assert.NotNull(response);
        Activity span = Assert.Single(spanCapture.Stopped, a => a.OperationName == "conversation_client");
        Assert.Equal("sendActivity", span.GetTagItem("operation"));
        Assert.Equal("conv-1", span.GetTagItem("conversation.id"));
        Assert.Equal("sent-1", span.GetTagItem("activity.id"));

        Assert.Equal(1, metricCapture.GetCounterTotal("teams.outbound.calls"));
        Assert.Equal(0, metricCapture.GetCounterTotal("teams.outbound.errors"));
    }

    [Fact]
    public async Task ConversationClient_SendActivityAsync_RecordsErrorOnFailure()
    {
        using SpanCapture spanCapture = new();
        using MetricCapture metricCapture = new();

        Mock<HttpMessageHandler> handler = new();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network down"));

        ConversationClient client = new(new HttpClient(handler.Object));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendActivityAsync("conv-1", new CoreActivity
        {
            Type = ActivityType.Message,
            ServiceUrl = new Uri("https://smba.example/"),
            Conversation = new("conv-1"),
        }, new Uri("https://smba.example/")));

        Activity span = Assert.Single(spanCapture.Stopped, a => a.OperationName == "conversation_client");
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal(1, metricCapture.GetCounterTotal("teams.outbound.errors"));
        Assert.Equal(0, metricCapture.GetCounterTotal("teams.outbound.calls"));
    }

    private static CoreActivity NewActivity() => new()
    {
        Type = ActivityType.Message,
        Id = "act-test",
        ChannelId = "msteams",
        ServiceUrl = new Uri("https://smba.example/"),
        Conversation = new("conv-test"),
    };

    private static DefaultHttpContext BuildHttpContext(CoreActivity activity)
    {
        DefaultHttpContext ctx = new();
        ctx.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(activity.ToJson()));
        ctx.Request.ContentType = "application/json";
        return ctx;
    }

    private static BotApplication CreateBotApplication()
    {
        ConversationClient cc = new(new HttpClient(Mock.Of<HttpMessageHandler>()));
        UserTokenClient ut = new(new HttpClient(Mock.Of<HttpMessageHandler>()), Mock.Of<IConfiguration>(), NullLogger<UserTokenClient>.Instance);
        return new BotApplication(cc, ut, NullLogger<BotApplication>.Instance);
    }

    /// <summary>
    /// Test harness: subscribes an <see cref="ActivityListener"/> to the SDK's source and records every span.
    /// </summary>
    private sealed class SpanCapture : IDisposable
    {
        private readonly ActivityListener _listener;
        public List<Activity> Stopped { get; } = [];

        public SpanCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == CoreTelemetryNames.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = a =>
                {
                    lock (Stopped) { Stopped.Add(a); }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose() => _listener.Dispose();
    }

    /// <summary>
    /// Test harness: subscribes a <see cref="MeterListener"/> to the SDK's meter and aggregates emitted measurements.
    /// </summary>
    private sealed class MetricCapture : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Dictionary<string, long> _counterTotals = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _histogramSamples = new(StringComparer.Ordinal);

        public MetricCapture()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == CoreTelemetryNames.MeterName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                },
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            {
                lock (_counterTotals)
                {
                    _counterTotals.TryGetValue(instrument.Name, out long total);
                    _counterTotals[instrument.Name] = total + value;
                }
            });
            _listener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
            {
                lock (_histogramSamples)
                {
                    _histogramSamples.TryGetValue(instrument.Name, out int count);
                    _histogramSamples[instrument.Name] = count + 1;
                }
            });
            _listener.Start();
        }

        public long GetCounterTotal(string name)
        {
            lock (_counterTotals)
            {
                return _counterTotals.TryGetValue(name, out long total) ? total : 0;
            }
        }

        public int HistogramSampleCount(string name)
        {
            lock (_histogramSamples)
            {
                return _histogramSamples.TryGetValue(name, out int count) ? count : 0;
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}
