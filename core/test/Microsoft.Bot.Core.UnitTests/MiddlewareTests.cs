using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.Bot.Core.UnitTests;

public class MiddlewareTests
{
    [Fact]
    public async Task BotApplication_Use_AddsMiddlewareToChain()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        var mockMiddleware = new Mock<ITurnMiddleWare>();

        // Act
        var result = botApp.Use(mockMiddleware.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Middleware_ExecutesInOrder()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        var executionOrder = new List<int>();

        var mockMiddleware1 = new Mock<ITurnMiddleWare>();
        mockMiddleware1
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                executionOrder.Add(1);
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        var mockMiddleware2 = new Mock<ITurnMiddleWare>();
        mockMiddleware2
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                executionOrder.Add(2);
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware1.Object);
        botApp.Use(mockMiddleware2.Object);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        var httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) =>
        {
            executionOrder.Add(3);
            return Task.CompletedTask;
        };

        // Act
        await botApp.ProcessAsync(httpContext);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
    }

    [Fact]
    public async Task Middleware_CanShortCircuit()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        bool secondMiddlewareCalled = false;
        bool onActivityCalled = false;

        var mockMiddleware1 = new Mock<ITurnMiddleWare>();
        mockMiddleware1
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Don't call next

        var mockMiddleware2 = new Mock<ITurnMiddleWare>();
        mockMiddleware2
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback(() => secondMiddlewareCalled = true)
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware1.Object);
        botApp.Use(mockMiddleware2.Object);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        var httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await botApp.ProcessAsync(httpContext);

        // Assert
        Assert.False(secondMiddlewareCalled);
        Assert.False(onActivityCalled);
    }

    [Fact]
    public async Task Middleware_ReceivesCancellationToken()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        CancellationToken receivedToken = default;

        var mockMiddleware = new Mock<ITurnMiddleWare>();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                receivedToken = ct;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware.Object);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        var httpContext = CreateHttpContextWithActivity(activity);

        var cts = new CancellationTokenSource();

        // Act
        await botApp.ProcessAsync(httpContext, cts.Token);

        // Assert
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task Middleware_ReceivesActivity()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        CoreActivity? receivedActivity = null;

        var mockMiddleware = new Mock<ITurnMiddleWare>();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                receivedActivity = act;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware.Object);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        var httpContext = CreateHttpContextWithActivity(activity);

        // Act
        await botApp.ProcessAsync(httpContext);

        // Assert
        Assert.NotNull(receivedActivity);
        Assert.Equal(ActivityTypes.Message, receivedActivity.Type);
        Assert.Equal("Test message", receivedActivity.Text);
    }

    private static ConversationClient CreateMockConversationClient()
    {
        var mockHttpClient = new Mock<HttpClient>();
        return new ConversationClient(mockHttpClient.Object);
    }

    private static HttpContext CreateHttpContextWithActivity(CoreActivity activity)
    {
        var httpContext = new DefaultHttpContext();
        var activityJson = activity.ToJson();
        var bodyBytes = Encoding.UTF8.GetBytes(activityJson);
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentType = "application/json";
        return httpContext;
    }
}
