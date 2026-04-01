// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core.Schema;
using Moq;

namespace Microsoft.Teams.Bot.Core.UnitTests;

public class MiddlewareTests
{
    [Fact]
    public async Task BotApplication_Use_AddsMiddlewareToChain()
    {
        BotApplication botApp = CreateBotApplication();

        Mock<ITurnMiddleware> mockMiddleware = new();

        ITurnMiddleware result = botApp.UseMiddleware(mockMiddleware.Object);

        Assert.NotNull(result);
    }


    [Fact]
    public async Task Middleware_ExecutesInOrder()
    {
        BotApplication botApp = CreateBotApplication();

        List<int> executionOrder = [];

        Mock<ITurnMiddleware> mockMiddleware1 = new();
        mockMiddleware1
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                executionOrder.Add(1);
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        Mock<ITurnMiddleware> mockMiddleware2 = new();
        mockMiddleware2
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                executionOrder.Add(2);
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.UseMiddleware(mockMiddleware1.Object);
        botApp.UseMiddleware(mockMiddleware2.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) =>
        {
            executionOrder.Add(3);
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(httpContext);
        int[] expected = [1, 2, 3];
        Assert.Equal(expected, executionOrder);
    }

    [Fact]
    public async Task Middleware_CanShortCircuit()
    {
        BotApplication botApp = CreateBotApplication();

        bool secondMiddlewareCalled = false;
        bool onActivityCalled = false;

        Mock<ITurnMiddleware> mockMiddleware1 = new();
        mockMiddleware1
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Don't call next

        Mock<ITurnMiddleware> mockMiddleware2 = new();
        mockMiddleware2
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback(() => secondMiddlewareCalled = true)
            .Returns(Task.CompletedTask);

        botApp.UseMiddleware(mockMiddleware1.Object);
        botApp.UseMiddleware(mockMiddleware2.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };
        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        botApp.OnActivity = (act, ct) =>
        {
            onActivityCalled = true;
            return Task.CompletedTask;
        };

        await botApp.ProcessAsync(httpContext);

        Assert.False(secondMiddlewareCalled);
        Assert.False(onActivityCalled);
    }

    [Fact]
    public async Task Middleware_ReceivesCancellationToken()
    {
        BotApplication botApp = CreateBotApplication();

        CancellationToken receivedToken = default;

        Mock<ITurnMiddleware> mockMiddleware = new();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                receivedToken = ct;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.UseMiddleware(mockMiddleware.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };

        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        CancellationTokenSource cts = new();

        await botApp.ProcessAsync(httpContext, cts.Token);

        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task Middleware_ReceivesActivity()
    {
        BotApplication botApp = CreateBotApplication();

        CoreActivity? receivedActivity = null;

        Mock<ITurnMiddleware> mockMiddleware = new();
        mockMiddleware
            .Setup(m => m.OnTurnAsync(It.IsAny<BotApplication>(), It.IsAny<CoreActivity>(), It.IsAny<NextTurn>(), It.IsAny<CancellationToken>()))
            .Callback<BotApplication, CoreActivity, NextTurn, CancellationToken>(async (app, act, next, ct) =>
            {
                receivedActivity = act;
                await next(ct);
            })
            .Returns(Task.CompletedTask);

        botApp.UseMiddleware(mockMiddleware.Object);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act123"
        };

        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        await botApp.ProcessAsync(httpContext);

        Assert.NotNull(receivedActivity);
        Assert.Equal(ActivityType.Message, receivedActivity.Type);
    }

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
        Mock<IConfiguration> mockConfig = new();
        NullLogger<UserTokenClient> logger = NullLogger<UserTokenClient>.Instance;
        return new UserTokenClient(mockHttpClient.Object, mockConfig.Object, logger);
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
