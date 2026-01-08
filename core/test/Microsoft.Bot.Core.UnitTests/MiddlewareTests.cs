// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.Bot.Core.UnitTests;

public class MiddlewareTests
{
    [Fact]
    public async Task BotApplication_Use_AddsMiddlewareToChain()
    {
        ConversationClient conversationClient = CreateMockConversationClient();
        Mock<IConfiguration> mockConfig = new();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, mockConfig.Object, logger);

        Mock<ITurnMiddleWare> mockMiddleware = new();

        ITurnMiddleWare result = botApp.Use(mockMiddleware.Object);

        Assert.NotNull(result);
    }


    [Fact]
    public async Task Middleware_ExecutesInOrder()
    {
        ConversationClient conversationClient = CreateMockConversationClient();
        Mock<IConfiguration> mockConfig = new();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, mockConfig.Object, logger);

        List<int> executionOrder = [];

        Mock<ITurnMiddleWare> mockMiddleware1 = new();
        mockMiddleware1
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                executionOrder.Add(1);
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        Mock<ITurnMiddleWare> mockMiddleware2 = new();
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

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) =>
        {
            executionOrder.Add(3);
            return Task.FromResult<InvokeResponse?>(null);
        };

        await botApp.ProcessAsync(httpContext);
        int[] expected = [1, 2, 3];
        Assert.Equal(expected, executionOrder);
    }

    [Fact]
    public async Task Middleware_CanShortCircuit()
    {
        ConversationClient conversationClient = CreateMockConversationClient();
        Mock<IConfiguration> mockConfig = new();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, mockConfig.Object, logger);

        bool secondMiddlewareCalled = false;
        bool onActivityCalled = false;

        Mock<ITurnMiddleWare> mockMiddleware1 = new();
        mockMiddleware1
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Don't call next

        Mock<ITurnMiddleWare> mockMiddleware2 = new();
        mockMiddleware2
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback(() => secondMiddlewareCalled = true)
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware1.Object);
        botApp.Use(mockMiddleware2.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.FromResult<InvokeResponse?>(null);
        };

        await botApp.ProcessAsync(httpContext);

        Assert.False(secondMiddlewareCalled);
        Assert.False(onActivityCalled);
    }

    [Fact]
    public async Task Middleware_ReceivesCancellationToken()
    {
        ConversationClient conversationClient = CreateMockConversationClient();
        Mock<IConfiguration> mockConfig = new();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, mockConfig.Object, logger);

        CancellationToken receivedToken = default;

        Mock<ITurnMiddleWare> mockMiddleware = new();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                receivedToken = ct;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        CancellationTokenSource cts = new();

        await botApp.ProcessAsync(httpContext, cts.Token);

        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task Middleware_ReceivesActivity()
    {
        ConversationClient conversationClient = CreateMockConversationClient();
        Mock<IConfiguration> mockConfig = new();
        NullLogger<BotApplication> logger = NullLogger<BotApplication>.Instance;
        BotApplication botApp = new(conversationClient, mockConfig.Object, logger);

        CoreActivity? receivedActivity = null;

        Mock<ITurnMiddleWare> mockMiddleware = new();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                receivedActivity = act;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.Use(mockMiddleware.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        activity.Recipient.Properties["appId"] = "test-app-id";

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        await botApp.ProcessAsync(httpContext);

        Assert.NotNull(receivedActivity);
        Assert.Equal(ActivityType.Message, receivedActivity.Type);
    }

    private static ConversationClient CreateMockConversationClient()
    {
        Mock<HttpClient> mockHttpClient = new();
        return new ConversationClient(mockHttpClient.Object);
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
