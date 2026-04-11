// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
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

        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

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
        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

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

        if (activity.Recipient is not null)
        {
            activity.Recipient.Properties["appId"] = "test-app-id";
        }

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

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        SendActivityResponse? result = await botApp.SendActivityAsync(activity);

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

    [Fact]
    public async Task ProcessAsync_WithActivityHandlerOverride_UsesProvidedHandlerNotSharedField()
    {
        // Arrange
        BotApplication botApp = CreateBotApplication();

        // Set a sentinel on the shared field to detect if it is incorrectly invoked
        bool sharedFieldInvoked = false;
        botApp.OnActivity = (_, _) =>
        {
            sharedFieldInvoked = true;
            return Task.CompletedTask;
        };

        CoreActivity activity = new() { Type = ActivityType.Message, Id = "act-override" };
        DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

        bool perRequestHandlerInvoked = false;
        Func<CoreActivity, CancellationToken, Task> perRequestHandler = (_, _) =>
        {
            perRequestHandlerInvoked = true;
            return Task.CompletedTask;
        };

        // Act
        await botApp.ProcessAsync(httpContext, perRequestHandler, CancellationToken.None);

        // Assert – only the per-request handler must fire; the shared field must not
        Assert.True(perRequestHandlerInvoked);
        Assert.False(sharedFieldInvoked);
    }

    [Fact]
    public async Task ProcessAsync_ConcurrentRequests_EachHandlerReceivesItsOwnActivity()
    {
        // Arrange
        BotApplication botApp = CreateBotApplication();

        int concurrency = 20;
        ConcurrentDictionary<string, string> observedActivities = new();

        // Act – fire concurrently; each request supplies its own scoped handler
        await Parallel.ForEachAsync(Enumerable.Range(0, concurrency), async (i, ct) =>
        {
            string activityId = $"act-{i}";
            CoreActivity activity = new() { Type = ActivityType.Message, Id = activityId };
            DefaultHttpContext httpContext = CreateHttpContextWithActivity(activity);

            Func<CoreActivity, CancellationToken, Task> handler = (act, _) =>
            {
                observedActivities[activityId] = act.Id!;
                return Task.CompletedTask;
            };

            await botApp.ProcessAsync(httpContext, handler, ct);
        });

        // Assert – every handler must have received exactly its own activity (no cross-contamination)
        Assert.Equal(concurrency, observedActivities.Count);
        for (int i = 0; i < concurrency; i++)
        {
            string expected = $"act-{i}";
            Assert.Equal(expected, observedActivities[expected]);
        }
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
