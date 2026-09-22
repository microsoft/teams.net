// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeNegotiatorTests
{
    private const string BotToken = "bot-token";
    private static readonly Uri NegotiateUri = new("https://botapi.skype.com/amer/v3/websockets/connect");

    [Theory]
    [InlineData("https://botapi.skype.com/v3/websockets/connect")]
    [InlineData("http://localhost:5000/v3/websockets/connect")]
    [InlineData("http://127.0.0.1:5000/v3/websockets/connect")]
    [InlineData("http://[::1]:5000/v3/websockets/connect")]
    public async Task NegotiateAsync_AllowsSecureAndLoopbackUris(string uri)
    {
        RecordingHandler handler = SuccessHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        SocketModeNegotiateResponse response = await negotiator.NegotiateAsync(new Uri(uri));

        Assert.Equal("https://signalr.example.test/client", response.Url);
        Assert.Equal(1, handler.SendCount);
    }

    [Theory]
    [InlineData("http://botapi.skype.com/v3/websockets/connect")]
    [InlineData("http://127.0.0.2/v3/websockets/connect")]
    [InlineData("ftp://botapi.skype.com/v3/websockets/connect")]
    public async Task NegotiateAsync_RejectsInsecureRemoteUris(string uri)
    {
        RecordingHandler handler = SuccessHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => negotiator.NegotiateAsync(new Uri(uri)));

        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task NegotiateAsync_RejectsRelativeUri()
    {
        RecordingHandler handler = SuccessHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => negotiator.NegotiateAsync(new Uri("/v3/websockets/connect", UriKind.Relative)));

        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task NegotiateAsync_SendsAuthenticatedPostWithoutBody()
    {
        RecordingHandler handler = SuccessHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        await negotiator.NegotiateAsync(NegotiateUri);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(NegotiateUri, handler.RequestUri);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal(BotToken, handler.Authorization?.Parameter);
        Assert.False(handler.HadContent);
    }

    [Theory]
    [InlineData("""
        {
          "url": "https://signalr.example.test/client",
          "accessToken": "signalr-token",
          "expiresIn": 3600
        }
        """)]
    [InlineData("""
        {
          "Url": "https://signalr.example.test/client",
          "AccessToken": "signalr-token",
          "ExpiresIn": 3600
        }
        """)]
    public async Task NegotiateAsync_DeserializesSuccessfulResponse(string json)
    {
        RecordingHandler handler = JsonHandler(HttpStatusCode.OK, json);
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        SocketModeNegotiateResponse response = await negotiator.NegotiateAsync(NegotiateUri);

        Assert.Equal("https://signalr.example.test/client", response.Url);
        Assert.Equal("signalr-token", response.AccessToken);
        Assert.Equal(3600, response.ExpiresIn);
    }

    [Theory]
    [InlineData("""{ "accessToken": "signalr-token" }""")]
    [InlineData("""{ "url": "https://signalr.example.test/client" }""")]
    [InlineData("""{ "url": "", "accessToken": "signalr-token" }""")]
    [InlineData("""{ "url": "https://signalr.example.test/client", "accessToken": "" }""")]
    public async Task NegotiateAsync_RejectsMissingResponseFields(string json)
    {
        RecordingHandler handler = JsonHandler(HttpStatusCode.OK, json);
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => negotiator.NegotiateAsync(NegotiateUri));
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("")]
    public async Task NegotiateAsync_RejectsInvalidResponseJson(string json)
    {
        RecordingHandler handler = JsonHandler(HttpStatusCode.OK, json);
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => negotiator.NegotiateAsync(NegotiateUri));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NegotiateAsync_RejectsMissingBotTokenWithoutSendingRequest(string? token)
    {
        RecordingHandler handler = SuccessHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(handler, _ => Task.FromResult(token));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.Equal(0, handler.SendCount);
    }

    [Fact]
    public async Task NegotiateAsync_ThrowsTypedExceptionWithDeltaRetryAfter()
    {
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(12));
            return Task.FromResult(response);
        });
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        SocketModeNegotiateException exception =
            await Assert.ThrowsAsync<SocketModeNegotiateException>(
                () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(12), exception.RetryAfter);
    }

    [Fact]
    public async Task NegotiateAsync_ParsesDateRetryAfter()
    {
        DateTimeOffset now = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(now.AddSeconds(30));
            return Task.FromResult(response);
        });
        SocketModeNegotiator negotiator = CreateNegotiator(
            handler,
            timeProvider: new FixedTimeProvider(now));

        SocketModeNegotiateException exception =
            await Assert.ThrowsAsync<SocketModeNegotiateException>(
                () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.Equal(TimeSpan.FromSeconds(30), exception.RetryAfter);
    }

    [Fact]
    public async Task NegotiateAsync_ClampsPastRetryAfterDateToZero()
    {
        DateTimeOffset now = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        RecordingHandler handler = new((_, _) =>
        {
            HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(now.AddSeconds(-30));
            return Task.FromResult(response);
        });
        SocketModeNegotiator negotiator = CreateNegotiator(
            handler,
            timeProvider: new FixedTimeProvider(now));

        SocketModeNegotiateException exception =
            await Assert.ThrowsAsync<SocketModeNegotiateException>(
                () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.Equal(TimeSpan.Zero, exception.RetryAfter);
    }

    [Fact]
    public async Task NegotiateAsync_LeavesRetryAfterNullWhenAbsent()
    {
        RecordingHandler handler = new((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        SocketModeNegotiateException exception =
            await Assert.ThrowsAsync<SocketModeNegotiateException>(
                () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.Null(exception.RetryAfter);
    }

    [Fact]
    public async Task NegotiateAsync_HonorsCallerCancellation()
    {
        RecordingHandler handler = HangingHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(handler);
        using CancellationTokenSource cancellationSource = new();

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => negotiator.NegotiateAsync(NegotiateUri, cancellationSource.Token));
    }

    [Fact]
    public async Task NegotiateAsync_TimesOutHangingRequest()
    {
        RecordingHandler handler = HangingHandler();
        SocketModeNegotiator negotiator = CreateNegotiator(
            handler,
            timeout: TimeSpan.FromMilliseconds(50));

        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(
            () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.Contains("timed out", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NegotiateAsync_DoesNotExposeSecretsInServiceFailure()
    {
        const string signalRToken = "signalr-secret";
        RecordingHandler handler = JsonHandler(
            HttpStatusCode.BadGateway,
            $$"""{ "accessToken": "{{signalRToken}}" }""");
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        SocketModeNegotiateException exception =
            await Assert.ThrowsAsync<SocketModeNegotiateException>(
                () => negotiator.NegotiateAsync(NegotiateUri));

        Assert.DoesNotContain(BotToken, exception.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(signalRToken, exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("http://signalr.example.test/client")]
    [InlineData("ftp://signalr.example.test/client")]
    [InlineData("/relative/client")]
    public async Task NegotiateAsync_RejectsInvalidSignalRUrl(string signalRUrl)
    {
        RecordingHandler handler = JsonHandler(
            HttpStatusCode.OK,
            $$"""
                {
                  "url": "{{signalRUrl}}",
                  "accessToken": "signalr-token"
                }
                """);
        SocketModeNegotiator negotiator = CreateNegotiator(handler);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => negotiator.NegotiateAsync(NegotiateUri));
    }

    private static SocketModeNegotiator CreateNegotiator(
        RecordingHandler handler,
        Func<CancellationToken, Task<string?>>? getBotToken = null,
        TimeSpan? timeout = null,
        TimeProvider? timeProvider = null)
    {
        HttpClient httpClient = new(handler);
        return new SocketModeNegotiator(
            httpClient,
            getBotToken ?? (_ => Task.FromResult<string?>(BotToken)),
            timeout,
            timeProvider);
    }

    private static RecordingHandler SuccessHandler()
        => JsonHandler(
            HttpStatusCode.OK,
            """
            {
              "url": "https://signalr.example.test/client",
              "accessToken": "signalr-token",
              "expiresIn": 3600
            }
            """);

    private static RecordingHandler JsonHandler(HttpStatusCode statusCode, string json)
        => new((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        }));

    private static RecordingHandler HangingHandler()
        => new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        internal int SendCount { get; private set; }
        internal HttpMethod? Method { get; private set; }
        internal Uri? RequestUri { get; private set; }
        internal AuthenticationHeaderValue? Authorization { get; private set; }
        internal bool HadContent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization;
            HadContent = request.Content is not null;
            return send(request, cancellationToken);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
