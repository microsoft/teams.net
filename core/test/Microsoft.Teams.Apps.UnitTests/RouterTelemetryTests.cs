// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Diagnostics;

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
            Name = TeamsActivityType.Message,
            Selector = _ => true,
            Handler = (_, _) => Task.CompletedTask,
        });

        await router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityType.Message }));

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
            Name = TeamsActivityType.Invoke,
            Selector = _ => true,
            HandlerWithReturn = (_, _) => Task.FromResult(new InvokeResponse(200)),
        });

        InvokeResponse response = await router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityType.Invoke, Name = "tab/fetch" }));
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
            Name = $"{TeamsActivityType.Invoke}/tab/fetch",
            Selector = _ => true,
            HandlerWithReturn = (_, _) => Task.FromResult(new InvokeResponse(200)),
        });

        await router.DispatchWithReturnAsync(BuildCtx(new InvokeActivity { Type = TeamsActivityType.Invoke, Name = "tab/fetch" }));

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
            Name = TeamsActivityType.Message,
            Selector = _ => true,
            Handler = (_, _) => throw new InvalidOperationException("handler failed"),
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            router.DispatchAsync(BuildCtx(new MessageActivity { Type = TeamsActivityType.Message })));

        Activity span = Assert.Single(capture.Stopped, a => a.OperationName == "handler");
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Contains(span.Events, e => e.Name == "exception");
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
}
