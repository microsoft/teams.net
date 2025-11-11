using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Testing.Plugins;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.Teams.Apps.Tests.Events;

public class EventTests
{
    private readonly Mock<ILogger<App>> _logger = new();
    private readonly App _app;
    private readonly TestPlugin _plugin;
    private readonly IToken _token;

    public EventTests()
    {
        _app = new App(_logger.Object);
        _plugin = new TestPlugin();
        _app.AddPlugin(_plugin);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnEvent()
    {
        var calls = 0;

        _app.OnEvent("activity", (sender, @event) =>
        {
            calls++;
            Assert.True(@event is ActivityEvent);
        });

        _app.OnEvent("test.activity", (sender, @event) =>
        {
            calls++;
            Assert.True(@event is ActivityEvent);
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Should_CallHandler_OnEvent_Async()
    {
        var calls = 0;

        _app.OnEvent("activity", (sender, @event, _) =>
        {
            calls++;
            Assert.True(@event is ActivityEvent);
            return Task.CompletedTask;
        });

        _app.OnEvent("test.activity", (sender, @event, _) =>
        {
            calls++;
            Assert.True(@event is ActivityEvent);
            return Task.CompletedTask;
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
    }
}