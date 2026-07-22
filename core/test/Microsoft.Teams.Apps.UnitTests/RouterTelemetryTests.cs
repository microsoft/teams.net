// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class RouterTelemetryTests
{
    [Fact]
    public async Task DispatchAsync_EmitsHandlerSpanWithTypeDispatch()
    {
        using SpanCapture capture = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<MessageActivity>
        {
            Name = TeamsActivityTypes.Message,
            Selector = _ => true,
            Handler = (_, _) => Task.CompletedTask,
        });

        await router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityTypes.Message }));

        Activity span = Assert.Single(capture.Stopped, a => a.OperationName == "handler");
        Assert.Equal("message", span.GetTagItem("handler.type"));
        Assert.Equal("type", span.GetTagItem("handler.dispatch"));
    }

    [Fact]
    public async Task DispatchWithReturnAsync_InvokeCatchallEmitsCatchallDispatch()
    {
        using SpanCapture capture = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = TeamsActivityTypes.Invoke,
            Selector = _ => true,
            HandlerWithReturn = (_, _) => Task.FromResult(new InvokeResponse(200)),
        });

        InvokeResponse response = await router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityTypes.Invoke, Name = "tab/fetch" }));
        Assert.Equal(200, response.Status);

        Activity span = Assert.Single(capture.Stopped, a => a.OperationName == "handler");
        Assert.Equal("invoke", span.GetTagItem("handler.type"));
        Assert.Equal("catchall", span.GetTagItem("handler.dispatch"));
    }

    [Fact]
    public async Task DispatchWithReturnAsync_SpecificInvokeEmitsInvokeDispatch()
    {
        using SpanCapture capture = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = $"{TeamsActivityTypes.Invoke}/tab/fetch",
            Selector = _ => true,
            HandlerWithReturn = (_, _) => Task.FromResult(new InvokeResponse(200)),
        });

        await router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityTypes.Invoke, Name = "tab/fetch" }));

        Activity span = Assert.Single(capture.Stopped, a => a.OperationName == "handler");
        Assert.Equal("tab/fetch", span.GetTagItem("handler.type"));
        Assert.Equal("invoke", span.GetTagItem("handler.dispatch"));
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrows_RecordsExceptionOnSpan()
    {
        using SpanCapture capture = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<MessageActivity>
        {
            Name = TeamsActivityTypes.Message,
            Selector = _ => true,
            Handler = (_, _) => throw new InvalidOperationException("handler failed"),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityTypes.Message })));

        Activity span = Assert.Single(capture.Stopped, a => a.OperationName == "handler");
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Contains(span.Events, e => e.Name == "exception");
    }

    [Fact]
    public async Task DispatchAsync_RecordsDispatchedAndDurationMetrics()
    {
        using MetricCapture metrics = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<MessageActivity>
        {
            Name = TeamsActivityTypes.Message,
            Selector = _ => true,
            Handler = (_, _) => Task.CompletedTask,
        });

        await router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityTypes.Message }));

        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.dispatched"));
        Assert.Equal(1, metrics.HistogramSampleCount("teams.handler.duration"));
        Assert.Equal(0, metrics.GetCounterTotal("teams.handler.failures"));
        Assert.Equal(0, metrics.GetCounterTotal("teams.handler.unmatched"));

        IReadOnlyList<KeyValuePair<string, object?>> dispatchedTags = metrics.GetCounterTags("teams.handler.dispatched");
        Assert.Contains(new KeyValuePair<string, object?>("handler.type", "message"), dispatchedTags);
        Assert.Contains(new KeyValuePair<string, object?>("handler.dispatch", "type"), dispatchedTags);
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrows_RecordsFailureMetric()
    {
        using MetricCapture metrics = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<MessageActivity>
        {
            Name = TeamsActivityTypes.Message,
            Selector = _ => true,
            Handler = (_, _) => throw new InvalidOperationException("handler failed"),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityTypes.Message })));

        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.dispatched"));
        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.failures"));
        // Duration is recorded even on exception (via finally block).
        Assert.Equal(1, metrics.HistogramSampleCount("teams.handler.duration"));

        IReadOnlyList<KeyValuePair<string, object?>> failureTags = metrics.GetCounterTags("teams.handler.failures");
        Assert.Contains(new KeyValuePair<string, object?>("handler.type", "message"), failureTags);
        Assert.Contains(new KeyValuePair<string, object?>("handler.dispatch", "type"), failureTags);
    }

    [Fact]
    public async Task DispatchAsync_NoMatchingRoute_RecordsUnmatchedMetric()
    {
        using MetricCapture metrics = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<MessageActivity>
        {
            Name = TeamsActivityTypes.Message,
            Selector = _ => false,
            Handler = (_, _) => Task.CompletedTask,
        });

        await router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityTypes.Message }));

        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.unmatched"));
        Assert.Equal(0, metrics.GetCounterTotal("teams.handler.dispatched"));

        IReadOnlyList<KeyValuePair<string, object?>> unmatchedTags = metrics.GetCounterTags("teams.handler.unmatched");
        Assert.Contains(new KeyValuePair<string, object?>("activity.type", "message"), unmatchedTags);
    }

    [Fact]
    public async Task DispatchWithReturnAsync_RecordsDispatchedAndDurationMetrics()
    {
        using MetricCapture metrics = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = $"{TeamsActivityTypes.Invoke}/tab/fetch",
            Selector = _ => true,
            HandlerWithReturn = (_, _) => Task.FromResult(new InvokeResponse(200)),
        });

        await router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityTypes.Invoke, Name = "tab/fetch" }));

        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.dispatched"));
        Assert.Equal(1, metrics.HistogramSampleCount("teams.handler.duration"));
        Assert.Equal(0, metrics.GetCounterTotal("teams.handler.failures"));

        IReadOnlyList<KeyValuePair<string, object?>> dispatchedTags = metrics.GetCounterTags("teams.handler.dispatched");
        Assert.Contains(new KeyValuePair<string, object?>("handler.type", "tab/fetch"), dispatchedTags);
        Assert.Contains(new KeyValuePair<string, object?>("handler.dispatch", "invoke"), dispatchedTags);
    }

    [Fact]
    public async Task DispatchWithReturnAsync_HandlerThrows_RecordsFailureMetric()
    {
        using MetricCapture metrics = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = $"{TeamsActivityTypes.Invoke}/tab/fetch",
            Selector = _ => true,
            HandlerWithReturn = (_, _) => throw new InvalidOperationException("invoke failed"),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityTypes.Invoke, Name = "tab/fetch" })));

        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.dispatched"));
        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.failures"));
        Assert.Equal(1, metrics.HistogramSampleCount("teams.handler.duration"));
    }

    [Fact]
    public async Task DispatchWithReturnAsync_NoMatchingInvoke_RecordsUnmatchedMetric()
    {
        using MetricCapture metrics = new();

        Router router = new(NullLogger.Instance);
        router.Register(new Route<InvokeActivity>
        {
            Name = $"{TeamsActivityTypes.Invoke}/tab/fetch",
            Selector = _ => false,
            HandlerWithReturn = (_, _) => Task.FromResult(new InvokeResponse(200)),
        });

        InvokeResponse response = await router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityTypes.Invoke, Name = "tab/fetch" }));

        Assert.Equal(501, response.Status);
        Assert.Equal(1, metrics.GetCounterTotal("teams.handler.unmatched"));
        Assert.Equal(0, metrics.GetCounterTotal("teams.handler.dispatched"));

        IReadOnlyList<KeyValuePair<string, object?>> unmatchedTags = metrics.GetCounterTags("teams.handler.unmatched");
        Assert.Contains(new KeyValuePair<string, object?>("activity.type", "invoke"), unmatchedTags);
        Assert.Contains(new KeyValuePair<string, object?>("invoke.name", "tab/fetch"), unmatchedTags);
    }

    private static Context<TeamsActivity> BuildCtx(TeamsActivity activity) => new(null!, activity);

    private sealed class SpanCapture : IDisposable
    {
        private readonly ActivityListener _listener;
        public List<Activity> Stopped { get; } = [];

        public SpanCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == TeamsBotApplicationTelemetry.ActivitySourceName,
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
    /// Test harness: subscribes a <see cref="MeterListener"/> to the Apps meter and aggregates
    /// emitted measurements (counter totals, histogram sample counts, and the most recent tag set
    /// per instrument).
    /// </summary>
    private sealed class MetricCapture : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Dictionary<string, long> _counterTotals = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _histogramSamples = new(StringComparer.Ordinal);
        private readonly Dictionary<string, KeyValuePair<string, object?>[]> _lastTags = new(StringComparer.Ordinal);

        public MetricCapture()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == TeamsBotApplicationTelemetry.MeterName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                },
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            {
                lock (_counterTotals)
                {
                    _counterTotals.TryGetValue(instrument.Name, out long total);
                    _counterTotals[instrument.Name] = total + value;
                    _lastTags[instrument.Name] = tags.ToArray();
                }
            });
            _listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
            {
                lock (_histogramSamples)
                {
                    _histogramSamples.TryGetValue(instrument.Name, out int count);
                    _histogramSamples[instrument.Name] = count + 1;
                    _lastTags[instrument.Name] = tags.ToArray();
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

        public IReadOnlyList<KeyValuePair<string, object?>> GetCounterTags(string name)
        {
            lock (_counterTotals)
            {
                return _lastTags.TryGetValue(name, out KeyValuePair<string, object?>[]? tags) ? tags : [];
            }
        }

        public void Dispose() => _listener.Dispose();
    }
}
