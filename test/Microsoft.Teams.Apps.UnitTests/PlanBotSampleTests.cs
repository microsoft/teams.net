// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using PlanBot;

namespace Microsoft.Teams.Apps.UnitTests;

public class PlanBotSampleTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("complete")]
    [InlineData("complete-stream")]
    public async Task Completion_ShowcasesVersionedStepsSourcesAndFinalOnlyCompletion(string command)
    {
        using Sample sample = new(command);
        Task handling = sample.Demo.HandleAsync(sample.Context, CancellationToken.None);
        for (int i = 0; i < 4; i++) await sample.Time.AdvanceAsync(2);
        if (command == "complete-stream")
            for (int i = 0; i < 3; i++) await sample.Time.AdvanceAsync(0.5);
        await handling.WaitAsync(TestTimeout);

        JsonNode[] plans = sample.Handler.Bodies.Select(PlanEntity).OfType<JsonNode>().ToArray();
        Assert.Equal(["start", "continue", "continue", "continue", "complete"],
            plans.Select(plan => plan["planType"]!.GetValue<string>()));
        AssertStep(plans[0]["steps"]![0]!, "lookup", 1, "Checking sources", "active",
            "Looking for relevant information in the Teams SDK documentation.");
        AssertStep(plans[0]["steps"]![1]!, "answer", 1, "Draft answer", "proposed",
            "Summarize the findings once the sources have been checked.");
        AssertStep(plans[1]["steps"]![0]!, "lookup", 2, "Waiting for source response", "blocked",
            "The source has not responded yet; waiting before continuing.");
        AssertStep(plans[2]["steps"]![0]!, "lookup", 3, "Sources checked", "completed",
            "Found the relevant Teams SDK documentation for the answer.");
        AssertStep(plans[2]["steps"]![1]!, "answer", 2, "Drafting answer", "active",
            "Turning the source information into a concise response.");
        AssertStep(plans[3]["steps"]![0]!, "answer", 3, "Answer ready", "completed",
            command == "complete" ? "The response is ready to send." : "The response is ready to stream.");
        JsonNode source = Assert.Single(plans[2]["steps"]![0]!["sources"]!.AsArray())!;
        Assert.Equal("Teams SDK docs", source["title"]!.GetValue<string>());
        Assert.Equal("https://microsoft.github.io/teams-sdk/", source["url"]!.GetValue<string>());
        Assert.Equal("Text", source["icon"]!.GetValue<string>());

        JsonNode final = Assert.Single(sample.Handler.Bodies, body => body["type"]!.GetValue<string>() == "message");
        Assert.Equal("Here is the answer based on the sources.", final["text"]!.GetValue<string>());
        Assert.Same(plans[^1], PlanEntity(final));
        if (command == "complete-stream")
        {
            JsonNode[] chunks = sample.Handler.Bodies.Where(body => PlanEntity(body) is null).ToArray();
            Assert.NotEmpty(chunks);
            Assert.All(chunks, chunk => Assert.Equal("typing", chunk["type"]!.GetValue<string>()));
            Assert.Contains(final["entities"]!.AsArray(), entity => entity?["type"]?.GetValue<string>() == "streaminfo");
        }
        AssertTargeting(sample);
    }

    [Fact]
    public async Task Stop_StopsAnActiveLookupWithoutCompletingTheWork()
    {
        using Sample sample = new("stop");
        Task handling = sample.Demo.HandleAsync(sample.Context, CancellationToken.None);
        await sample.Time.AdvanceAsync(5);
        await handling.WaitAsync(TestTimeout);
        JsonNode[] bodies = sample.Handler.Bodies.ToArray();
        Assert.Equal(2, bodies.Length);
        JsonNode start = PlanEntity(bodies[0])!;
        Assert.Equal("start", start["planType"]!.GetValue<string>());
        AssertStep(Assert.Single(start["steps"]!.AsArray())!, "lookup", 1, "Checking sources", "active",
            "Looking for relevant information in the Teams SDK documentation.");
        Assert.Equal("stop", PlanEntity(bodies[1])!["planType"]!.GetValue<string>());
        Assert.All(bodies, body => Assert.Equal("typing", body["type"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Error_ShowsFailedStepBeforeTerminatingPlan()
    {
        using Sample sample = new("error");
        Task handling = sample.Demo.HandleAsync(sample.Context, CancellationToken.None);
        await sample.Time.AdvanceAsync(2);
        await sample.Time.AdvanceAsync(2);
        await handling.WaitAsync(TestTimeout);
        JsonNode[] plans = sample.Handler.Bodies.Select(body => PlanEntity(body)!).ToArray();
        Assert.Equal(["start", "continue", "error"], plans.Select(plan => plan["planType"]!.GetValue<string>()));
        Assert.Single(plans[0]["steps"]!.AsArray());
        AssertStep(Assert.Single(plans[1]["steps"]!.AsArray())!, "lookup", 2, "Source lookup failed", "error",
            "The source could not be retrieved, so the lookup cannot continue.");
        Assert.All(sample.Handler.Bodies, body => Assert.Equal("typing", body["type"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Proactive_ReturnsThenReconstructsOriginalBindingAndRemovesFinishedTask()
    {
        using Sample sample = new("proactive");
        using CancellationTokenSource handlerCancellation = new();
        await sample.Demo.HandleAsync(sample.Context, handlerCancellation.Token).WaitAsync(TestTimeout);
        Assert.Single(sample.Handler.Bodies);
        Assert.Equal(1, sample.Demo.PendingCount);
        handlerCancellation.Cancel();
        sample.Context.Activity.Id = "later-prompt";
        sample.Context.Activity.Conversation = TeamsConversation.FromConversation(new Conversation("later-chat"));

        await sample.Time.AdvanceAsync(10);
        await sample.Time.AdvanceAsync(2);
        await sample.Time.AdvanceAsync(10);
        await sample.DrainAsync();

        JsonNode[] plans = sample.Handler.Bodies.Select(body => PlanEntity(body)!).ToArray();
        Assert.Equal(["start", "continue", "continue", "complete"], plans.Select(plan => plan["planType"]!.GetValue<string>()));
        AssertStep(plans[0]["steps"]![0]!, "report", 1, "Preparing background report", "active",
            "This report continues in the background after the handler returns.");
        AssertStep(plans[1]["steps"]![0]!, "report", 2, "Report ready", "completed",
            "The background work is complete and the report is ready to send.");
        AssertStep(plans[1]["steps"]![1]!, "deliver", 1, "Delivering report", "active",
            "Finalizing delivery of the completed report back to the user.");
        AssertStep(plans[2]["steps"]![0]!, "deliver", 2, "Delivered report", "completed",
            "Finalized delivery of the completed report back to the user.");
        Assert.Equal("Your background report is ready.", sample.Handler.Bodies.Last()["text"]!.GetValue<string>());
        AssertTargeting(sample);
        Assert.Empty(sample.Logger.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Proactive_ObservesFailuresAndShutdownAndRemovesFinishedTask(bool shutdown)
    {
        using Sample sample = new("proactive");
        await sample.Demo.HandleAsync(sample.Context, CancellationToken.None).WaitAsync(TestTimeout);
        if (shutdown)
        {
            sample.Stopping.Cancel();
        }
        else
        {
            sample.Handler.FailNext = true;
            await sample.Time.AdvanceAsync(10);
        }
        await sample.DrainAsync();
        (LogLevel level, Exception? error) = Assert.Single(sample.Logger.Events);
        Assert.Equal(shutdown ? LogLevel.Information : LogLevel.Error, level);
        if (!shutdown) Assert.IsType<PlanConflictException>(error);
        Assert.Equal(shutdown ? 1 : 2, sample.Handler.Bodies.Count);
    }

    private static JsonNode? PlanEntity(JsonNode body)
        => body["entities"]?.AsArray().FirstOrDefault(entity => entity?["type"]?.GetValue<string>() == "planInfo");

    private static void AssertStep(JsonNode step, string id, int version, string title, string status, string description)
    {
        Assert.Equal(id, step["id"]!.GetValue<string>());
        Assert.Equal(version, step["version"]!.GetValue<int>());
        Assert.Equal(title, step["title"]!.GetValue<string>());
        Assert.Equal(status, step["status"]!.GetValue<string>());
        Assert.Equal(description, step["description"]!.GetValue<string>());
    }

    private static void AssertTargeting(Sample sample)
    {
        Assert.All(sample.Handler.Bodies, body => Assert.Equal("prompt-id", body["replyToId"]!.GetValue<string>()));
        Assert.All(sample.Handler.Requests, request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://example.com/v3/conversations/chat%3Bmessageid%3Dthread-root/activities/",
                request.RequestUri!.AbsoluteUri);
        });
    }

    private sealed class Sample : IDisposable
    {
        private readonly HttpClient _http;
        public RecordingHandler Handler { get; } = new();
        public RecordingLogger Logger { get; } = new();
        public ControlledTimeProvider Time { get; } = new();
        public CancellationTokenSource Stopping { get; } = new();
        public Context<MessageActivity> Context { get; }
        public PlanDemo Demo { get; }

        public Sample(string command)
        {
            _http = new HttpClient(Handler);
            ConversationClient conversation = new(_http, NullLogger<ConversationClient>.Instance);
            UserTokenClient token = new(_http, new ConfigurationBuilder().Build(), NullLogger<UserTokenClient>.Instance);
            ApiClient api = new(new Uri("https://example.com/"), _http, conversation, token);
            TeamsBotApplication app = new(api, new HttpContextAccessor(), NullLogger<TeamsBotApplication>.Instance,
                new TeamsBotApplicationOptions { AppId = "app-id" });
            MessageActivity activity = MessageActivity.FromActivity(new CoreActivity
            {
                Id = "prompt-id",
                Type = ActivityType.Message,
                Conversation = new Conversation("chat;messageid=thread-root"),
                ServiceUrl = new Uri("https://example.com/"),
                Recipient = new ChannelAccount { Id = "bot" }
            });
            activity.Text = $"<at>PlanBot</at> {command.ToUpperInvariant()} ";
            activity.Entities = [new MentionEntity(new ChannelAccount { Id = "bot" }, "<at>PlanBot</at>")];
            Context = new Context<MessageActivity>(app, activity);
            Demo = new PlanDemo(Logger, Stopping.Token, Time);
        }

        public async Task DrainAsync()
        {
            await Demo.WaitForPendingAsync().WaitAsync(TestTimeout);
            using CancellationTokenSource timeout = new(TestTimeout);
            while (Demo.PendingCount != 0) await Task.Delay(1, timeout.Token);
        }

        public void Dispose()
        {
            Stopping.Cancel();
            Stopping.Dispose();
            _http.Dispose();
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public ConcurrentQueue<JsonNode> Bodies { get; } = new();
        public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();
        public bool FailNext { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Enqueue(request);
            Bodies.Enqueue(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!);
            bool fail = FailNext;
            FailNext = false;
            return new HttpResponseMessage(fail ? HttpStatusCode.Conflict : HttpStatusCode.Created)
            {
                Content = new StringContent(fail
                    ? """{"error":{"code":"PlanConflict","message":"Stopped"}}"""
                    : """{"id":"answer-id"}""", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        public ConcurrentQueue<(LogLevel, Exception?)> Events { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Events.Enqueue((logLevel, exception));
    }

    private sealed class ControlledTimeProvider : TimeProvider
    {
        private readonly Channel<(TimeSpan Delay, ManualTimer Timer)> _timers =
            Channel.CreateUnbounded<(TimeSpan, ManualTimer)>();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ManualTimer timer = new(callback, state);
            Assert.True(_timers.Writer.TryWrite((dueTime, timer)));
            return timer;
        }

        public async Task AdvanceAsync(double expectedSeconds)
        {
            using CancellationTokenSource timeout = new(TestTimeout);
            (TimeSpan delay, ManualTimer timer) = await _timers.Reader.ReadAsync(timeout.Token);
            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
            timer.Fire();
        }

        private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
        {
            private int _disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Fire()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) callback(state);
            }
            public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
