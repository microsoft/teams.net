using System.Net;
using System.Text;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Testing.Plugins;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Apps.Tests.Events;

public class ErrorEventTests
{
    private readonly App _app;
    private readonly TestPlugin _plugin;
    private readonly IToken _token;

    private class CustomException(string message) : Exception(message)
    {

    }

    public ErrorEventTests()
    {
        _app = new App();
        _plugin = new TestPlugin();
        _app.AddPlugin(_plugin);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnErrorEvent()
    {
        var calls = 0;

        _app.OnEvent(EventType.Error, (sender, @event) =>
        {
            calls++;
            Assert.True(@event is ErrorEvent error && error.Exception is CustomException custom && custom.Message == "testing123");
        });

        _app.OnError((sender, @event) =>
        {
            calls++;
            Assert.True(@event is not null);
            Assert.True(@event.Exception is CustomException custom && custom.Message == "testing123");
        });

        _app.OnActivity((_, @event) =>
        {
            throw new CustomException("testing123");
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(HttpStatusCode.InternalServerError, res.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Should_NotCallHandler_OnErrorEvent()
    {
        var calls = 0;

        _app.OnEvent(EventType.Error, (sender, @event) => calls++);
        _app.OnError((sender, @event) => calls++);
        _app.OnActivity((_, @event) =>
        {
            Assert.True(@event.Activity is MessageActivity message && message.Text == "hello world");
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Should_CallHandler_OnErrorEvent_On_HttpException()
    {
        var calls = 0;
        var json = """{ "message": "some error content" }""";

        _app.OnEvent(EventType.Error, (sender, @event) =>
        {
            calls++;

            if (@event is not ErrorEvent error)
            {
                Assert.Fail();
                return;
            }

            if (error.Exception is not HttpException ex)
            {
                Assert.Fail($"received unexpected type {error.Exception.GetType()}");
                return;
            }

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        });

        _app.OnError((sender, @event) =>
        {
            calls++;

            if (@event.Exception is not HttpException ex)
            {
                Assert.Fail($"received unexpected type {@event.Exception.GetType()}");
                return;
            }

            Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        });

        _app.OnMessage(context =>
        {
            var res = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            throw new HttpException()
            {
                Headers = res.Headers,
                StatusCode = res.StatusCode,
                Body = res.Content
            };
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(HttpStatusCode.InternalServerError, res.Status);
        Assert.Equal(2, calls);
    }
}