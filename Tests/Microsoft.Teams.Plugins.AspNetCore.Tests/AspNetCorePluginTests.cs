using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Common.Logging;
using Moq;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests;

public class AspNetCorePluginTests
{
    private static AspNetCorePlugin CreatePlugin(Mock<ILogger>? loggerMock = null, EventFunction? events = null)
    {
        var plugin = new AspNetCorePlugin();
        if (loggerMock is not null)
        {
            plugin.Logger = loggerMock.Object;
        }
        else
        {
            plugin.Logger = new ConsoleLogger("Test", LogLevel.Debug);
        }
        plugin.Client = new Mock<Microsoft.Teams.Common.Http.IHttpClient>().Object;
        if (events is not null)
        {
            plugin.Events += events;
        }
        return plugin;
    }

    private static DefaultHttpContext CreateHttpContext(IActivity activity, string bearer = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQ3MDI1MTUyMDB9.signature")
    {
        var ctx = new DefaultHttpContext();
        ctx.TraceIdentifier = Guid.NewGuid().ToString();
        ctx.Request.Headers.Append("Authorization", $"Bearer {bearer}");
        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx;
    }

    private static MessageActivity CreateMessageActivity()
    {
        return new MessageActivity("hi")
        {
            From = new() { Id = "user" },
            Recipient = new() { Id = "bot" },
            Conversation = new Conversation() { Id = "conv", Type = ConversationType.Personal }
        };
    }

    [Fact]
    public async Task Test_Do_Http_CallsExtractTokenAndActivity_AndCallsCoreDo()
    {
        // Arrange
        var activity = CreateMessageActivity();
        var coreResponse = new Response(HttpStatusCode.Accepted, new { ok = true });
        var eventsCalled = new List<string>();

        EventFunction events = (plugin, name, payload, ct) =>
        {
            eventsCalled.Add(name);
            if (name == "activity") return Task.FromResult<object?>(coreResponse); // returned directly by core Do
            return Task.FromResult<object?>(null);
        };

        var logger = new Mock<ILogger>();
        var plugin = CreatePlugin(logger, events);
        var ctx = CreateHttpContext(activity);

        // Act
        var result = await plugin.Do(ctx);

        // Assert
        Assert.Contains("activity", eventsCalled);
        var jsonResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<object?>>(result);
        Assert.Equal((int)coreResponse.Status, jsonResult.StatusCode);
    }

    [Fact]
    public async Task Test_Do_Http_SetsHeadersFromResponseMeta()
    {
        // Arrange
        var activity = CreateMessageActivity();
        var response = new Response(HttpStatusCode.OK, new { hello = "world" });
        response.Meta.Add("routes", 3);
        response.Meta.Add("custom", "value");

        EventFunction events = (plugin, name, payload, ct) =>
        {
            if (name == "activity") return Task.FromResult<object?>(response);
            return Task.FromResult<object?>(null);
        };

        var plugin = CreatePlugin(new Mock<ILogger>(), events);
        var ctx = CreateHttpContext(activity);

        // Act
        var result = await plugin.Do(ctx);

        // Assert body result type
        var jsonResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<object?>>(result);
        Assert.Equal((int)HttpStatusCode.OK, jsonResult.StatusCode);
        // Headers: routes & custom should be present
        Assert.Contains("X-Teams-Routes", ctx.Response.Headers.Keys); // capitalized first char
        Assert.Contains("X-Teams-Custom", ctx.Response.Headers.Keys);
    }

    [Fact]
    public async Task Test_Do_Http_ErrorPath_ProducesProblemResult()
    {
        // Arrange -> throw inside events
        EventFunction events = (plugin, name, payload, ct) =>
        {
            if (name == "activity") throw new InvalidOperationException("boom");
            return Task.FromResult<object?>(null);
        };

        var logger = new Mock<ILogger>();
        var plugin = CreatePlugin(logger, events);
        var ctx = CreateHttpContext(CreateMessageActivity());

        // Act
        var result = await plugin.Do(ctx);

        // Assert
        var problem = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<object>>(result);
        Assert.Equal(500, problem.StatusCode);
        Assert.Contains("boom", problem.Value!.ToString());
        logger.Verify(l => l.Error(It.IsAny<object[]>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Test_ExtractToken_ReturnsToken()
    {
        var plugin = CreatePlugin();
        var ctx = CreateHttpContext(CreateMessageActivity(), "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQ3MDI1MTUyMDB9.token123");

        var token = plugin.ExtractToken(ctx.Request);
        Assert.NotNull(token);
        Assert.Contains("token123", token.ToString());
    }

    [Fact]
    public async Task Test_ExtractActivity_ReturnsActivity()
    {
        var plugin = CreatePlugin();
        var activity = CreateMessageActivity();
        var ctx = CreateHttpContext(activity);

        var extracted = await plugin.ParseActivity(ctx.Request);
        Assert.NotNull(extracted);
        Assert.True(activity.Type.Equals(extracted.Type));
    }

    [Fact]
    public async Task Test_ExtractActivity_HttpRequestBodyAlreadyRead_ReturnsActivity()
    {
        var plugin = CreatePlugin();
        var activity = CreateMessageActivity();
        var ctx = CreateHttpContext(activity);
        // simulate body already read by setting position to end
        ctx.Request.Body.Position = ctx.Request.Body.Length;

        var extracted = await plugin.ParseActivity(ctx.Request);
        Assert.NotNull(extracted);
        Assert.True(activity.Type.Equals(extracted.Type));
    }

    [Fact]
    public async Task Test_Do_Core_ReturnsResponseAndLogs()
    {
        // Arrange core path tests the ActivityEvent Do(ActivityEvent)
        var response = new Response(HttpStatusCode.OK, new { test = 1 });
        EventFunction events = (plugin, name, payload, ct) =>
        {
            if (name == "activity") return Task.FromResult<object?>(response);
            return Task.FromResult<object?>(null);
        };
        var logger = new Mock<ILogger>();
        var plugin = CreatePlugin(logger, events);
        var evt = new ActivityEvent() { Token = new JsonWebToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQ3MDI1MTUyMDB9.signature"), Activity = CreateMessageActivity() };

        // Act
        var res = await plugin.Do(evt);

        // Assert
        Assert.Same(response, res);
        logger.Verify(l => l.Debug(It.IsAny<object[]>()), Times.AtLeastOnce);
    }
}
