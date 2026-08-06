// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Routing;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Moq;
using System.Text;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsBotApplicationTests
{
    [Fact]
    public async Task Reply_Proactive_ThrowsOnInvalidMessageId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("19:abc@thread.skype", "not-a-number", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_ThrowsOnZeroMessageId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("19:abc@thread.skype", "0", "hello"));
    }

    [Fact]
    public async Task Reply_Proactive_ThrowsOnEmptyConversationId()
    {
        TeamsBotApplication app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            app.ReplyAsync("", "1680000000000", "hello"));
    }

    [Fact]
    public void HasMatchingRoute_ReturnsTrueForRegisteredInvokeHandler()
    {
        TeamsBotApplication app = CreateApp();
        app.OnInvoke((_, _) => Task.FromResult(InvokeResponse.Ok()));
        CoreActivity activity = new(TeamsActivityTypes.Invoke);
        activity.Properties["name"] = InvokeNames.TaskFetch;

        Assert.True(app.HasMatchingRoute(activity));
        Assert.False(app.HasMatchingRoute(new CoreActivity(ActivityType.Message)));
        Assert.Equal(InvokeNames.TaskFetch, activity.Properties["name"]);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsResponseFromMatchedInvokeHandler()
    {
        TeamsBotApplication app = CreateApp();
        app.OnInvoke((_, _) => Task.FromResult(InvokeResponse.Ok()));

        InvokeResponse? response = await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch));

        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
    }

    [Fact]
    public async Task ProcessAsync_ExecutesMiddlewareBeforeInvokeHandler()
    {
        TeamsBotApplication app = CreateApp();
        List<string> executionOrder = [];
        app.UseMiddleware(new TestMiddleware(async (_, _, next, cancellationToken) =>
        {
            executionOrder.Add("middleware");
            await next(cancellationToken);
        }));
        app.OnInvoke((_, _) =>
        {
            executionOrder.Add("handler");
            return Task.FromResult(InvokeResponse.Ok());
        });

        await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch));

        Assert.Equal(["middleware", "handler"], executionOrder);
    }

    [Fact]
    public async Task ProcessAsync_MiddlewareCanShortCircuitInvokeHandler()
    {
        TeamsBotApplication app = CreateApp();
        bool handlerCalled = false;
        app.UseMiddleware(new TestMiddleware((_, _, _, _) => Task.CompletedTask));
        app.OnInvoke((_, _) =>
        {
            handlerCalled = true;
            return Task.FromResult(InvokeResponse.Ok());
        });

        InvokeResponse? response = await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch));

        Assert.Null(response);
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task ProcessAsync_DispatchesNonInvokeActivityAndReturnsNull()
    {
        TeamsBotApplication app = CreateApp();
        bool handlerCalled = false;
        app.Router.Register(new Route<MessageActivity>
        {
            Name = TeamsActivityTypes.Message,
            Selector = _ => true,
            Handler = (_, _) =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            },
        });

        InvokeResponse? response = await app.ProcessAsync(new CoreActivity(ActivityType.Message));

        Assert.Null(response);
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task HttpProcessAsync_WritesInvokeResponse()
    {
        HttpContextAccessor accessor = new();
        TeamsBotApplication app = CreateApp(accessor);
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, new { result = "accepted" })));
        DefaultHttpContext httpContext = new();
        accessor.HttpContext = httpContext;
        byte[] activityJson = Encoding.UTF8.GetBytes(new InvokeActivity(InvokeNames.TaskFetch).ToJson());
        httpContext.Request.Body = new MemoryStream(activityJson);
        httpContext.Response.Body = new MemoryStream();

        await app.ProcessAsync(httpContext);

        Assert.Equal(202, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body, Encoding.UTF8);
        Assert.Contains("accepted", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task ActivityProcessAsync_DoesNotWriteInvokeResponseToAmbientHttpContext()
    {
        HttpContextAccessor accessor = new();
        TeamsBotApplication app = CreateApp(accessor);
        app.OnInvoke((_, _) => Task.FromResult(new InvokeResponse(202, new { result = "accepted" })));
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();
        accessor.HttpContext = httpContext;

        InvokeResponse? response = await app.ProcessAsync(new InvokeActivity(InvokeNames.TaskFetch));

        Assert.NotNull(response);
        Assert.Equal(202, response.Status);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private static TeamsBotApplication CreateApp()
        => CreateApp(new HttpContextAccessor());

    private static TeamsBotApplication CreateApp(IHttpContextAccessor httpContextAccessor)
    {
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);

        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        return new TeamsBotApplication(
            apiClient,
            httpContextAccessor,
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
    }

    private sealed class TestMiddleware(
        Func<BotApplication, CoreActivity, NextTurn, CancellationToken, Task> onTurn) : ITurnMiddleware
    {
        public Task OnTurnAsync(
            BotApplication botApplication,
            CoreActivity activity,
            NextTurn nextTurn,
            CancellationToken cancellationToken = default)
            => onTurn(botApplication, activity, nextTurn, cancellationToken);
    }
}
