// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Verifies that <see cref="OAuthFlow"/> emits the OTel spans and metrics described in
/// <c>core/docs/sso/OAuthFlow-Design.md</c> and <c>core/docs/Observability-Design.md</c>.
/// </summary>
public class OAuthFlowTelemetryTests
{
    private const string GraphConnection = "graph";
    private const string TestUserId = "user-1";
    private const string TestChannelId = "msteams";

    // ==================== GetTokenAsync ====================

    [Fact]
    public async Task GetTokenAsync_CachedToken_EmitsHitResult()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "tok", ConnectionName = GraphConnection });

        Context<MessageActivity> ctx = CreateMessageContext(harness);
        string? token = await harness.Flow.GetTokenAsync(ctx);

        Assert.Equal("tok", token);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthGetToken);
        Assert.Equal(AppsTelemetry.OAuthOperations.GetToken, span.GetTagItem(AppsTelemetry.Tags.OAuthOperation));
        Assert.Equal(AppsTelemetry.OAuthResults.Hit, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(GraphConnection, span.GetTagItem(AppsTelemetry.Tags.OAuthConnection));

        Assert.Equal(1, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthOperations));
        Assert.Equal(1, metrics.HistogramSampleCount(AppsTelemetry.Metrics.OAuthOperationDuration));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task GetTokenAsync_NoToken_EmitsMissResult()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTokenResult?)null);

        Context<MessageActivity> ctx = CreateMessageContext(harness);
        string? token = await harness.Flow.GetTokenAsync(ctx);

        Assert.Null(token);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthGetToken);
        Assert.Equal(AppsTelemetry.OAuthResults.Miss, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    // ==================== SignInAsync ====================

    [Fact]
    public async Task SignInAsync_CachedToken_EmitsCachedResultWithoutCardEvent()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "cached", ConnectionName = GraphConnection });

        Context<MessageActivity> ctx = CreateMessageContext(harness);
        string? token = await harness.Flow.SignInAsync(ctx);

        Assert.Equal("cached", token);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthSignIn);
        Assert.Equal(AppsTelemetry.OAuthResults.Cached, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.DoesNotContain(span.Events, e => e.Name == AppsTelemetry.OAuthEvents.CardSent);

        Assert.Equal(1, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthOperations));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task SignInAsync_NoCachedToken_EmitsCardSentResultAndCardSentEvent()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness);
        string? token = await harness.Flow.SignInAsync(ctx);

        Assert.Null(token);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthSignIn);
        Assert.Equal(AppsTelemetry.OAuthResults.CardSent, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Contains(span.Events, e => e.Name == AppsTelemetry.OAuthEvents.CardSent);
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    // ==================== SignOutAsync ====================

    [Fact]
    public async Task SignOutAsync_EmitsSuccessResult()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.SignOutUserAsync(TestUserId, GraphConnection, TestChannelId, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Context<MessageActivity> ctx = CreateMessageContext(harness);
        await harness.Flow.SignOutAsync(ctx);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthSignOut);
        Assert.Equal(AppsTelemetry.OAuthResults.Success, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(1, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthOperations));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    // ==================== GetConnectionStatusAsync ====================

    [Fact]
    public async Task GetConnectionStatusAsync_TagsConnectionAsAll()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenStatusAsync(TestUserId, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new GetTokenStatusResult { ConnectionName = GraphConnection, HasToken = true } });

        Context<MessageActivity> ctx = CreateMessageContext(harness);
        IList<GetTokenStatusResult> statuses = await harness.Flow.GetConnectionStatusAsync(ctx);

        Assert.Single(statuses);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthConnectionStatus);
        Assert.Equal(AppsTelemetry.OAuthAllConnections, span.GetTagItem(AppsTelemetry.Tags.OAuthConnection));
        Assert.Equal(AppsTelemetry.OAuthOperations.ConnectionStatus, span.GetTagItem(AppsTelemetry.Tags.OAuthOperation));
        Assert.Equal(AppsTelemetry.OAuthResults.Success, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));

        IReadOnlyList<KeyValuePair<string, object?>> tags = metrics.GetCounterTags(AppsTelemetry.Metrics.OAuthOperations);
        Assert.Contains(new KeyValuePair<string, object?>(AppsTelemetry.Tags.OAuthConnection, AppsTelemetry.OAuthAllConnections), tags);
    }

    // ==================== HandleTokenExchangeAsync ====================

    [Fact]
    public async Task TokenExchange_Success_EmitsSuccessResult()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "access", ConnectionName = GraphConnection });

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-success", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> ctx = CreateInvokeContext(harness);

        InvokeResponse response = await harness.Flow.HandleTokenExchangeAsync(ctx, exchangeValue, CancellationToken.None);

        Assert.Equal(200, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthTokenExchange);
        Assert.Equal(AppsTelemetry.OAuthResults.Success, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(200, span.GetTagItem(AppsTelemetry.Tags.InvokeResponseStatus));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task TokenExchange_Duplicate_EmitsDuplicateResultAndNoError()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "access", ConnectionName = GraphConnection });

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-dup", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> ctx = CreateInvokeContext(harness);

        // First call: success; second call (same exchange id): duplicate
        await harness.Flow.HandleTokenExchangeAsync(ctx, exchangeValue, CancellationToken.None);
        InvokeResponse second = await harness.Flow.HandleTokenExchangeAsync(ctx, exchangeValue, CancellationToken.None);

        Assert.Equal(200, second.Status);

        List<Activity> exchangeSpans = spans.Stopped
            .Where(a => a.OperationName == AppsTelemetry.Spans.OAuthTokenExchange)
            .ToList();
        Assert.Equal(2, exchangeSpans.Count);
        Assert.Contains(exchangeSpans, s => string.Equals(s.GetTagItem(AppsTelemetry.Tags.OAuthResult) as string, AppsTelemetry.OAuthResults.Duplicate, StringComparison.Ordinal));

        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task TokenExchange_ExpectedFallback_Returns412AndDoesNotIncrementErrors()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest));

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-bad", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> ctx = CreateInvokeContext(harness);

        InvokeResponse response = await harness.Flow.HandleTokenExchangeAsync(ctx, exchangeValue, CancellationToken.None);

        Assert.Equal(412, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthTokenExchange);
        Assert.Equal(AppsTelemetry.OAuthResults.Failure, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Null(span.GetTagItem(AppsTelemetry.Tags.OAuthErrorType));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task TokenExchange_UnexpectedStatus_IncrementsErrorsWithHttpErrorType()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden));

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-forbidden", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> ctx = CreateInvokeContext(harness);

        InvokeResponse response = await harness.Flow.HandleTokenExchangeAsync(ctx, exchangeValue, CancellationToken.None);

        Assert.Equal(403, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthTokenExchange);
        Assert.Equal(AppsTelemetry.OAuthResults.Failure, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(AppsTelemetry.OAuthErrorTypes.HttpError, span.GetTagItem(AppsTelemetry.Tags.OAuthErrorType));

        Assert.Equal(1, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
        IReadOnlyList<KeyValuePair<string, object?>> errorTags = metrics.GetCounterTags(AppsTelemetry.Metrics.OAuthErrors);
        Assert.Contains(new KeyValuePair<string, object?>(AppsTelemetry.Tags.OAuthErrorType, AppsTelemetry.OAuthErrorTypes.HttpError), errorTags);
        Assert.Contains(new KeyValuePair<string, object?>(AppsTelemetry.Tags.OAuthOperation, AppsTelemetry.OAuthOperations.TokenExchange), errorTags);
    }

    // ==================== HandleVerifyStateAsync ====================

    [Fact]
    public async Task VerifyState_NullState_EmitsFailureResultWithoutError()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        Context<InvokeActivity> ctx = CreateInvokeContext(harness);
        SignInVerifyStateValue verifyValue = new() { State = null };

        InvokeResponse response = await harness.Flow.HandleVerifyStateAsync(ctx, verifyValue, CancellationToken.None);

        Assert.Equal(404, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthVerifyState);
        Assert.Equal(AppsTelemetry.OAuthResults.Failure, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task VerifyState_NoToken_EmitsNoTokenResultAndDoesNotIncrementErrors()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "code", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTokenResult?)null);

        Context<InvokeActivity> ctx = CreateInvokeContext(harness);
        SignInVerifyStateValue verifyValue = new() { State = "code" };

        InvokeResponse response = await harness.Flow.HandleVerifyStateAsync(ctx, verifyValue, CancellationToken.None);

        Assert.Equal(412, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthVerifyState);
        Assert.Equal(AppsTelemetry.OAuthResults.NoToken, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task VerifyState_ExpectedFallback_DoesNotIncrementErrors()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "code", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest));

        Context<InvokeActivity> ctx = CreateInvokeContext(harness);
        SignInVerifyStateValue verifyValue = new() { State = "code" };

        InvokeResponse response = await harness.Flow.HandleVerifyStateAsync(ctx, verifyValue, CancellationToken.None);

        Assert.Equal(412, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthVerifyState);
        Assert.Equal(AppsTelemetry.OAuthResults.Failure, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Null(span.GetTagItem(AppsTelemetry.Tags.OAuthErrorType));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    [Fact]
    public async Task VerifyState_UnexpectedStatus_IncrementsErrorsWithHttpErrorType()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "code", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden));

        Context<InvokeActivity> ctx = CreateInvokeContext(harness);
        SignInVerifyStateValue verifyValue = new() { State = "code" };

        InvokeResponse response = await harness.Flow.HandleVerifyStateAsync(ctx, verifyValue, CancellationToken.None);

        Assert.Equal(403, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthVerifyState);
        Assert.Equal(AppsTelemetry.OAuthResults.Failure, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal(AppsTelemetry.OAuthErrorTypes.HttpError, span.GetTagItem(AppsTelemetry.Tags.OAuthErrorType));
        Assert.Equal(1, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    // ==================== HandleSignInFailureAsync ====================

    [Fact]
    public async Task SignInFailure_EmitsNotifiedResultWithFailureCodeTag()
    {
        using SpanCapture spans = new();
        using MetricCapture metrics = new();
        TestHarness harness = CreateHarness();

        Context<InvokeActivity> ctx = CreateInvokeContext(harness);
        SignInFailureValue failureValue = new() { Code = "resourcematchfailed", Message = "URI mismatch" };

        InvokeResponse response = await harness.Flow.HandleSignInFailureAsync(ctx, failureValue, CancellationToken.None);

        Assert.Equal(200, response.Status);

        Activity span = AssertOAuthSpan(spans, AppsTelemetry.Spans.OAuthSignInFailure);
        Assert.Equal(AppsTelemetry.OAuthResults.Notified, span.GetTagItem(AppsTelemetry.Tags.OAuthResult));
        Assert.Equal("resourcematchfailed", span.GetTagItem(AppsTelemetry.Tags.OAuthFailureCode));
        Assert.Equal(0, metrics.GetCounterTotal(AppsTelemetry.Metrics.OAuthErrors));
    }

    // ==================== test scaffolding ====================

    private static Activity AssertOAuthSpan(SpanCapture capture, string operationName)
    {
        Activity? span = capture.Stopped
            .Where(a => a.OperationName == operationName)
            .Where(a => (a.GetTagItem(AppsTelemetry.Tags.OAuthConnection) as string) == GraphConnection
                        || (a.GetTagItem(AppsTelemetry.Tags.OAuthConnection) as string) == AppsTelemetry.OAuthAllConnections)
            .LastOrDefault();
        Assert.NotNull(span);
        return span!;
    }

    private static TestHarness CreateHarness()
    {
        Mock<UserTokenClient> mockUserTokenClient = CreateMockUserTokenClient();
        Mock<ConversationClient> mockConversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        TeamsBotApplication app = new(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        OAuthFlow flow = app.AddOAuthFlow(GraphConnection);

        return new TestHarness
        {
            App = app,
            MockUserTokenClient = mockUserTokenClient,
            MockConversationClient = mockConversationClient,
            Flow = flow,
        };
    }

    private static Mock<UserTokenClient> CreateMockUserTokenClient()
    {
        Mock<IConfiguration> mockConfig = new();
        return new Mock<UserTokenClient>(
            new HttpClient(),
            mockConfig.Object,
            NullLogger<UserTokenClient>.Instance);
    }

    private static Context<MessageActivity> CreateMessageContext(TestHarness harness)
    {
        MessageActivity activity = new("hello")
        {
            ChannelId = TestChannelId,
            From = new TeamsChannelAccount { Id = TestUserId },
            Recipient = new TeamsChannelAccount { Id = "bot-id" },
            Conversation = new TeamsConversation { Id = "conv-1" },
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
        };

        return new Context<MessageActivity>(harness.App, activity);
    }

    private static Context<InvokeActivity> CreateInvokeContext(TestHarness harness)
    {
        InvokeActivity activity = new()
        {
            ChannelId = TestChannelId,
            From = new TeamsChannelAccount { Id = TestUserId },
            Recipient = new TeamsChannelAccount { Id = "bot-id" },
            Conversation = new TeamsConversation { Id = "conv-1" },
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
        };

        return new Context<InvokeActivity>(harness.App, activity);
    }

    private static void SetupSilentTokenReturnsNull(Mock<UserTokenClient> mock)
    {
        mock.Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTokenResult?)null);
    }

    private static void SetupGetSignInResource(Mock<UserTokenClient> mock)
    {
        mock.Setup(c => c.GetSignInResourceAsync(It.IsAny<string>(), null, (Uri?)null, (Uri?)null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSignInResourceResult
            {
                SignInLink = "https://login.microsoftonline.com/test",
                TokenExchangeResource = new TokenExchangeResource { Id = "tex-1", Uri = new Uri("api://test") },
                TokenPostResource = new TokenPostResource { SasUrl = new Uri("https://token.botframework.com/test") }
            });
    }

    private static void SetupSendActivity(TestHarness harness)
    {
        harness.MockConversationClient
            .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<BotRequestContext?>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendActivityResponse { Id = "activity-1" });
    }

    private sealed class TestHarness
    {
        public required TeamsBotApplication App { get; init; }
        public required Mock<UserTokenClient> MockUserTokenClient { get; init; }
        public required Mock<ConversationClient> MockConversationClient { get; init; }
        public required OAuthFlow Flow { get; init; }
    }

    /// <summary>
    /// Subscribes to spans emitted on the <see cref="TeamsBotApplicationTelemetry.ActivitySourceName"/>
    /// source for the lifetime of the instance. Mirrors the helper used in <c>RouterTelemetryTests</c>.
    /// </summary>
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
    /// Subscribes a <see cref="MeterListener"/> to the Apps meter and aggregates emitted measurements
    /// (counter totals, histogram sample counts, and the most recent tag set per instrument).
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
