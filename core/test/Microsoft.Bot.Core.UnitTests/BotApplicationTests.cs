using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Microsoft.Bot.Core.UnitTests;

public class BotApplicationTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;

        // Act
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        // Assert
        Assert.NotNull(botApp);
        Assert.NotNull(botApp.ConversationClient);
    }



    [Fact]
    public async Task ProcessAsync_WithNullHttpContext_ThrowsArgumentNullException()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            botApp.ProcessAsync(null!));
    }

    [Fact]
    public async Task ProcessAsync_WithValidActivity_ProcessesSuccessfully()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        var httpContext = CreateHttpContextWithActivity(activity);

        bool onActivityCalled = false;
        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        // Act
        var result = await botApp.ProcessAsync(httpContext);

        // Assert
        Assert.NotNull(result);
        Assert.True(onActivityCalled);
        Assert.Equal(activity.Type, result.Type);
    }

    [Fact]
    public async Task ProcessAsync_WithMiddleware_ExecutesMiddleware()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        var httpContext = CreateHttpContextWithActivity(activity);

        bool middlewareCalled = false;
        var mockMiddleware = new Mock<ITurnMiddleWare>();
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

        // Act
        await botApp.ProcessAsync(httpContext);

        // Assert
        Assert.True(middlewareCalled);
        Assert.True(onActivityCalled);
    }

    [Fact]
    public async Task ProcessAsync_WithException_ThrowsBotHandlerException()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

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
            throw new InvalidOperationException("Test exception");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BotHandlerException>(() =>
            botApp.ProcessAsync(httpContext));

        Assert.Equal("Error processing activity", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void Use_AddsMiddlewareToChain()
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
    public async Task SendActivityAsync_WithValidActivity_SendsSuccessfully()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
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

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var conversationClient = new ConversationClient(httpClient);
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        // Act
        var result = await botApp.SendActivityAsync(activity);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("activity123", result);
    }

    [Fact]
    public async Task SendActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        var conversationClient = CreateMockConversationClient();
        var mockConfig = new Mock<IConfiguration>();
        var logger = NullLogger<BotApplication>.Instance;
        var botApp = new BotApplication(conversationClient, mockConfig.Object, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            botApp.SendActivityAsync(null!));
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
