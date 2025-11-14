using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Testing.Plugins;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Teams.Apps.Tests.Events;

public class ActivityEventTests
{
    private readonly App _app;
    private readonly TestPlugin _plugin;
    private readonly IToken _token;

    public ActivityEventTests()
    {
        _app = new App(NullLogger<App>.Instance);
        _plugin = new TestPlugin();
        _app.AddPlugin(_plugin);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnActivityEvent()
    {
        var calls = 0;

        _app.OnEvent("activity", (sender, @event) =>
        {
            calls++;
            Assert.True(@event is ActivityEvent);
        });

        _app.OnActivity((sender, @event) =>
        {
            calls++;
            Assert.True(@event is ActivityEvent);
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Should_PassExtra_OnActivityEvent()
    {
        IDictionary<string, object?>? extra = null;
        _app.OnEvent("activity", (sender, @event) =>
        {
            Assert.True(@event is ActivityEvent);
            extra = ((ActivityEvent)@event).Extra;
        });

        var extraFromParameter = new Dictionary<string, object?>
        {
            { "paramContextKey", "value" }
        };
        var res = await _plugin.Do(_token, new MessageActivity("hello world"), extraFromParameter);

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(extra!["paramContextKey"], extraFromParameter["paramContextKey"]);
    }

    [Fact]
    public async Task Should_CallHandler_OnActivityResponseEvent()
    {
        var calls = 0;

        _app.OnEvent("activity.response", (sender, @event) =>
        {
            calls++;
            Assert.True(@event is ActivityResponseEvent);
        });

        _app.OnActivityResponse((sender, @event) =>
        {
            calls++;
            Assert.True(@event is ActivityResponseEvent);
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Should_CallHandler_OnActivityResponseEvent_Async()
    {
        var calls = 0;

        _app.OnEvent("activity.response", (sender, @event, _) =>
        {
            calls++;
            Assert.True(@event is ActivityResponseEvent);
            return Task.CompletedTask;
        });

        _app.OnActivityResponse((sender, @event, _) =>
        {
            calls++;
            Assert.True(@event is ActivityResponseEvent);
            return Task.CompletedTask;
        });

        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
    }
}