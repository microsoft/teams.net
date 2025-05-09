using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Apps.Testing.Events;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Events;

public class CustomEventTests
{
    private readonly App _app;
    private readonly ISenderPlugin _plugin;
    private readonly IToken _token;

    public CustomEventTests()
    {
        _app = new App();
        _plugin = new TestPlugin();
        _app.AddPlugin(_plugin);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnEvent()
    {
        var calls = 0;

        _app.OnEvent("message", (sender, @event) =>
        {
            calls++;
            Assert.True(@event is TestMessageEvent);
        });

        _app.OnTestMessage((sender, @event) =>
        {
            calls++;
            Assert.True(@event is TestMessageEvent);
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Should_CallHandler_OnEvent_Async()
    {
        var calls = 0;

        _app.OnEvent("message", (sender, @event, _) =>
        {
            calls++;
            Assert.True(@event is TestMessageEvent);
            return Task.CompletedTask;
        });

        _app.OnTestMessage((sender, @event, _) =>
        {
            calls++;
            Assert.True(@event is TestMessageEvent);
            return Task.FromResult<object?>(null);
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
    }
}