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
    public async Task ProcessAsync_CoreActivity_NonInvoke_ReturnsNull()
    {
        BotApplication botApp = CreateBotApplication();
        bool onActivityCalled = false;
        botApp.OnActivity = (_, _) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        CoreInvokeResponse? response = await botApp.ProcessAsync(new CoreActivity(ActivityType.Message), user: null, correlationVector: null);

        Assert.True(onActivityCalled);
        Assert.Null(response);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_ReturnsInvokeResponseSetByHandler()
    {
        InvokeRecordingBot botApp = new();
        botApp.OnActivity = (_, _) =>
        {
            Assert.True(botApp.RecordInvokeResponse(new CoreInvokeResponse(200, new { hello = "world" })));
            return Task.CompletedTask;
        };

        CoreInvokeResponse? response = await botApp.ProcessAsync(new CoreActivity("invoke"), user: null, correlationVector: null);

        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Body);
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
    public async Task ProcessAsync_HttpContext_WritesInvokeResponseStatusAndJsonBody()
    {
        InvokeRecordingBot botApp = new();
        botApp.OnActivity = (_, _) =>
        {
            botApp.RecordInvokeResponse(new CoreInvokeResponse(201, new { hello = "world" }));
            return Task.CompletedTask;
        };
        DefaultHttpContext httpContext = CreateHttpContextWithActivity(new CoreActivity("invoke"));
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await botApp.ProcessAsync(httpContext);

        Assert.Equal(201, httpContext.Response.StatusCode);
        Assert.StartsWith("application/json", httpContext.Response.ContentType, StringComparison.Ordinal);
        Assert.Equal("{\"hello\":\"world\"}", Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    [Fact]
    public async Task ProcessAsync_HttpContext_InvokeResponseWithoutBody_WritesStatusOnly()
    {
        InvokeRecordingBot botApp = new();
        botApp.OnActivity = (_, _) =>
        {
            botApp.RecordInvokeResponse(new CoreInvokeResponse(501));
            return Task.CompletedTask;
        };
        DefaultHttpContext httpContext = CreateHttpContextWithActivity(new CoreActivity("invoke"));
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await botApp.ProcessAsync(httpContext);

        Assert.Equal(501, httpContext.Response.StatusCode);
        Assert.Equal(0, responseBody.Length);
    }

    [Fact]
    public async Task ProcessAsync_HttpContext_NoInvokeResponse_LeavesResponseUntouched()
    {
        BotApplication botApp = CreateBotApplication();
        botApp.OnActivity = (_, _) => Task.CompletedTask;
        DefaultHttpContext httpContext = CreateHttpContextWithActivity(new CoreActivity(ActivityType.Message));
        MemoryStream responseBody = new();
        httpContext.Response.Body = responseBody;

        await botApp.ProcessAsync(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Null(httpContext.Response.ContentType);
        Assert.Equal(0, responseBody.Length);
    }

    [Fact]
    public void TrySetInvokeResponse_OutsideTurn_ReturnsFalse()
    {
        InvokeRecordingBot botApp = new();

        Assert.False(botApp.RecordInvokeResponse(new CoreInvokeResponse(200)));
    }

    [Fact]
    public async Task TrySetInvokeResponse_AfterTurnCompletes_ReturnsFalse()
    {
        InvokeRecordingBot botApp = new();
        TaskCompletionSource turnEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>? lateSet = null;
        botApp.OnActivity = (_, _) =>
        {
            // Work started inside the turn inherits its async-local slot but runs after the turn has ended.
            lateSet = Task.Run(async () =>
            {
                await turnEnded.Task;
                return botApp.RecordInvokeResponse(new CoreInvokeResponse(200));
            });
            return Task.CompletedTask;
        };

        CoreInvokeResponse? response = await botApp.ProcessAsync(new CoreActivity("invoke"), user: null, correlationVector: null);
        turnEnded.SetResult();

        Assert.Null(response);
        Assert.NotNull(lateSet);
        Assert.False(await lateSet);
    }

    [Fact]
    public async Task TrySetInvokeResponse_FromAnotherApplication_ReturnsFalse()
    {
        InvokeRecordingBot botApp = new();
        InvokeRecordingBot otherApp = new();
        bool? otherAppResult = null;
        botApp.OnActivity = (_, _) =>
        {
            otherAppResult = otherApp.RecordInvokeResponse(new CoreInvokeResponse(500));
            return Task.CompletedTask;
        };

        CoreInvokeResponse? response = await botApp.ProcessAsync(new CoreActivity("invoke"), user: null, correlationVector: null);

        Assert.False(otherAppResult);
        Assert.Null(response);
    }

    [Fact]
    public async Task ProcessAsync_CoreActivity_ConcurrentTurns_ReturnOwnInvokeResponses()
    {
        InvokeRecordingBot botApp = new();
        TaskCompletionSource bothStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;
        botApp.OnActivity = async (activity, _) =>
        {
            if (Interlocked.Increment(ref started) == 2)
            {
                bothStarted.SetResult();
            }

            await bothStarted.Task;
            botApp.RecordInvokeResponse(new CoreInvokeResponse(200, activity.Id));
        };

        Task<CoreInvokeResponse?> first = botApp.ProcessAsync(new CoreActivity("invoke") { Id = "first" }, user: null, correlationVector: null);
        Task<CoreInvokeResponse?> second = botApp.ProcessAsync(new CoreActivity("invoke") { Id = "second" }, user: null, correlationVector: null);

        CoreInvokeResponse?[] responses = await Task.WhenAll(first, second);

        Assert.Equal("first", responses[0]?.Body);
        Assert.Equal("second", responses[1]?.Body);
    }

    private sealed class InvokeRecordingBot()
        : BotApplication(CreateMockConversationClient(), CreateMockUserTokenClient(), NullLogger<BotApplication>.Instance)
    {
        public bool RecordInvokeResponse(CoreInvokeResponse response) => TrySetInvokeResponse(response);
    }

    private static BotApplicationOptions CreateOptions(string appId) =>
        new() { AppId = appId };

    private static BotApplication CreateBotApplication() =>
        new(CreateMockConversationClient(), CreateMockUserTokenClient(), NullLogger<BotApplication>.Instance);

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
