// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Core.UnitTests;

public class BotApplicationTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        ConversationClient conversationClient = CreateMockConversationClient();
        UserTokenClient userTokenClient = CreateMockUserTokenClient();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;

        BotApplication botApp = new(conversationClient, userTokenClient, logger, CreateOptions("test-app-id"));
        Assert.NotNull(botApp);
        Assert.NotNull(botApp.ConversationClient);
        Assert.NotNull(botApp.UserTokenClient);
        Assert.NotNull(botApp.UserTokenClient);
    }



    [Fact]
    public async Task ProcessAsync_WithNullHttpContext_ThrowsArgumentNullException()
    {
        BotApplication botApp = CreateBotApplication();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            botApp.ProcessAsync(null!));
    }

    [Fact]
    public async Task ProcessAsync_WithValidActivity_ProcessesSuccessfully()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        activity.Properties["text"] = "Test message";


        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        bool onActivityCalled = false;
        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(httpContext);

        Assert.True(onActivityCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithMiddleware_ExecutesMiddleware()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        bool middlewareCalled = false;
        Mock<ITurnMiddleware> mockMiddleware = new();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                middlewareCalled = true;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.UseMiddleware(mockMiddleware.Object);

        bool onActivityCalled = false;
        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(httpContext);

        Assert.True(middlewareCalled);
        Assert.True(onActivityCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithException_ThrowsBotHandlerException()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };


        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) => throw new InvalidOperationException("Test exception");

        BotHandlerException exception = await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(httpContext));

        Assert.Equal("Error processing activity", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Use_AddsMiddlewareToChain()
    {
        BotApplication botApp = CreateBotApplication();

        Mock<ITurnMiddleware> mockMiddleware = new();

        ITurnMiddleware result = botApp.UseMiddleware(mockMiddleware.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendActivityAsync_WithValidActivity_SendsSuccessfully()
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"activity123\"}")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient);
        UserTokenClient userTokenClient = CreateMockUserTokenClient();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, userTokenClient, logger);

        CoreActivityInput activity = CoreActivityInput.CreateBuilder()
            .WithType(ActivityType.Message)
            .Build();
        SendActivityResponse? result = await botApp.SendActivityAsync("conv123", activity, new Uri("https://test.service.url/"));

        Assert.NotNull(result);
        Assert.Contains("activity123", result.Id);
    }

    [Fact]
    public async Task SendActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        BotApplication botApp = CreateBotApplication();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            botApp.SendActivityAsync("conv123", null!, new Uri("https://test.service.url/")));
    }

    [Fact]
    public async Task ProcessAsync_ServiceUrlClaimMatchesActivity_ProcessesSuccessfully()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/")
        };

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("serviceurl", "https://smba.trafficmanager.net/teams/")
        ]));

        bool onActivityCalled = false;
        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(httpContext);

        Assert.True(onActivityCalled);
    }


    [Fact]
    public async Task ProcessAsync_ServiceUrlClaimMismatch_ThrowsInvalidDataException()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/")
        };

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("serviceurl", "https://evil.example.com/")
        ]));

        botApp.OnActivity = (act, ct) => Task.CompletedTask;

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            botApp.ProcessAsync(httpContext));

        Assert.Contains("does not match", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_ServiceUrlClaimMismatchCase_ThrowsInvalidDataException()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/")
        };

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("serviceurl", "https://SMBA.trafficmanager.net/teams/")
        ]));

        botApp.OnActivity = (act, ct) => Task.CompletedTask;

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            botApp.ProcessAsync(httpContext));

        Assert.Contains("does not match", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_NoServiceUrlClaim_ProcessesSuccessfully()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/")
        };

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);
        // No serviceurl claim set — default ClaimsPrincipal has no claims

        bool onActivityCalled = false;
        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(httpContext);

        Assert.True(onActivityCalled);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_RunsMiddlewareAndOnActivity()
    {
        BotApplication botApp = CreateBotApplication();
        List<string> calls = [];
        Mock<ITurnMiddleware> middleware = new();
        middleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Returns<BotApplication, CoreActivity, NextTurn, CancellationToken>((_, _, next, ct) =>
            {
                calls.Add("middleware");
                return next(ct);
            });
        botApp.UseMiddleware(middleware.Object);
        CoreActivity activity = new(ActivityType.Message) { Id = "act123" };
        CoreActivity? received = null;
        botApp.OnActivity = (act, _) =>
        {
            calls.Add("onActivity");
            received = act;
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(activity, user: null, correlationVector: null);

        Assert.Equal(["middleware", "onActivity"], calls);
        Assert.Same(activity, received);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_ServiceUrlClaimMismatch_ThrowsInvalidDataException()
    {
        BotApplication botApp = CreateBotApplication();
        bool onActivityCalled = false;
        botApp.OnActivity = (_, _) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };
        CoreActivity activity = new(ActivityType.Message) { ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/") };
        ClaimsPrincipal user = new(new ClaimsIdentity([new Claim("serviceurl", "https://evil.example.com/")]));

        await Assert.ThrowsAsync<InvalidDataException>(() => botApp.ProcessAsync(activity, user, correlationVector: null));

        Assert.False(onActivityCalled);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_ServiceUrlClaimMatches_ProcessesSuccessfully()
    {
        BotApplication botApp = CreateBotApplication();
        bool onActivityCalled = false;
        botApp.OnActivity = (_, _) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };
        CoreActivity activity = new(ActivityType.Message) { ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/") };
        ClaimsPrincipal user = new(new ClaimsIdentity([new Claim("serviceurl", "https://smba.trafficmanager.net/teams/")]));

        await botApp.ProcessAsync(activity, user, correlationVector: null);

        Assert.True(onActivityCalled);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_HandlerThrows_ThrowsBotHandlerException()
    {
        BotApplication botApp = CreateBotApplication();
        botApp.OnActivity = (_, _) => throw new InvalidOperationException("Test exception");

        BotHandlerException exception = await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(new CoreActivity(ActivityType.Message), user: null, correlationVector: null));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_NullActivity_ThrowsArgumentNullException()
    {
        BotApplication botApp = CreateBotApplication();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            botApp.ProcessAsync((CoreActivity)null!, user: null, correlationVector: null));
    }

    [Fact]
    public async Task ProcessAsync_HttpContext_DelegatesToCoreActivityOverload()
    {
        RecordingBot botApp = new();
        CoreActivity activity = new(ActivityType.Message) { Id = "act123" };
        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);
        httpContext.Request.Headers["MS-CV"] = "cv-123";
        ClaimsPrincipal user = new(new ClaimsIdentity([new Claim("aud", "test-app-id")]));
        httpContext.User = user;

        await botApp.ProcessAsync(httpContext);

        Assert.Equal("act123", botApp.Activity?.Id);
        Assert.Same(user, botApp.User);
        Assert.Equal("cv-123", botApp.CorrelationVector);
    }

    private sealed class RecordingBot()
        : BotApplication(CreateMockConversationClient(), CreateMockUserTokenClient(), NullLogger<BotApplication>.Instance)
    {
        public CoreActivity? Activity { get; private set; }
        public ClaimsPrincipal? User { get; private set; }
        public string? CorrelationVector { get; private set; }

        public override Task ProcessAsync(CoreActivity activity, ClaimsPrincipal? user, string? correlationVector, CancellationToken cancellationToken = default)
        {
            Activity = activity;
            User = user;
            CorrelationVector = correlationVector;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_Timeout_ThrowsBotHandlerExceptionWithTimeoutInner()
    {
        BotApplication botApp = CreateBotApplication(TimeSpan.FromMilliseconds(50));
        // Bounded rather than infinite: with a debugger attached the processing timeout is disabled, so this fails instead of hanging.
        botApp.OnActivity = (_, ct) => Task.Delay(TimeSpan.FromSeconds(10), ct);
        CoreActivity activity = new(ActivityType.Message) { Id = "act123" };

        BotHandlerException exception = await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(activity, user: null, correlationVector: null));

        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.Same(activity, exception.Activity);
    }

    [Fact]
    public async Task ProcessAsync_HttpContext_Timeout_ThrowsBotHandlerException()
    {
        BotApplication botApp = CreateBotApplication(TimeSpan.FromMilliseconds(50));
        // Bounded rather than infinite: with a debugger attached the processing timeout is disabled, so this fails instead of hanging.
        botApp.OnActivity = (_, ct) => Task.Delay(TimeSpan.FromSeconds(10), ct);
        CoreActivity activity = new(ActivityType.Message) { Id = "act123" };
        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        BotHandlerException exception = await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(httpContext));

        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.Equal("act123", exception.Activity?.Id);
    }

    [Fact]
    public async Task ProcessAsync_HandlerThrowsTimeoutException_ThrowsBotHandlerException()
    {
        BotApplication botApp = CreateBotApplication();
        TimeoutException handlerException = new("handler timeout");
        botApp.OnActivity = (_, _) => throw handlerException;
        CoreActivity activity = new(ActivityType.Message) { Id = "act123" };

        BotHandlerException exception = await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(activity, user: null, correlationVector: null));

        Assert.Same(handlerException, exception.InnerException);
        Assert.Equal("Error processing activity", exception.Message);
        Assert.Same(activity, exception.Activity);
    }

    private static BotApplicationOptions CreateOptions(string appId) =>
        new() { AppId = appId };

    private static BotApplication CreateBotApplication(TimeSpan? processActivityTimeout = null) =>
        new(CreateMockConversationClient(), CreateMockUserTokenClient(), NullLogger<BotApplication>.Instance,
            processActivityTimeout is null ? null : new BotApplicationOptions { ProcessActivityTimeout = processActivityTimeout.Value });

    private static ConversationClient CreateMockConversationClient()
    {
        Mock<HttpClient> mockHttpClient = new();
        return new ConversationClient(mockHttpClient.Object);
    }

    private static UserTokenClient CreateMockUserTokenClient()
    {
        Mock<HttpClient> mockHttpClient = new();
        NullLogger<UserTokenClient> logger = NullLogger<UserTokenClient>.Instance;
        Mock<IConfiguration> mockConfiguration = new();
        return new UserTokenClient(mockHttpClient.Object, mockConfiguration.Object, logger);
    }

    private static DefaultHttpContext CreateHttpContextWithActivity(CoreActivity activity)
    {
        DefaultHttpContext httpContext = new();
        string activityJson = activity.ToJson();
        byte[] bodyBytes = Encoding.UTF8.GetBytes(activityJson);
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentType = "application/json";
        return httpContext;
    }
}
