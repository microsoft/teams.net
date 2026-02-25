// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core.Hosting;
using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Bot.Core.UnitTests;

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
        activity.Recipient.Properties["appId"] = "test-app-id";

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
        activity.Recipient.Properties["appId"] = "test-app-id";

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        bool middlewareCalled = false;
        Mock<ITurnMiddleWare> mockMiddleware = new();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                middlewareCalled = true;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware.Object);

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
        activity.Recipient.Properties["appId"] = "test-app-id";

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

        Mock<ITurnMiddleWare> mockMiddleware = new();

        ITurnMiddleWare result = botApp.Use(mockMiddleware.Object);

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

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        var result = await botApp.SendActivityAsync(activity);

        Assert.NotNull(result);
        Assert.Contains("activity123", result.Id);
    }

    [Fact]
    public async Task SendActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        BotApplication botApp = CreateBotApplication();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            botApp.SendActivityAsync(null!));
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
