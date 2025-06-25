using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Events;

public class ActivityEventTests
{
    private readonly App _app;
    private readonly ISenderPlugin _plugin;
    private readonly IToken _token;

    public ActivityEventTests()
    {
        _app = new App();
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
    public async Task Should_PassContextExtra_OnActivityEvent()
    {
        IDictionary<string, object>? onEventExtra = null;
        _app.OnEvent("activity", (sender, @event) =>
        {
            Assert.True(@event is ActivityEvent);
            onEventExtra = ((ActivityEvent)@event).ContextExtra;
        });

        IDictionary<string, object>? onActivityExtra = null;
        _app.OnActivity((sender, @event) =>
        {
            Assert.True(@event is ActivityEvent);
            onActivityExtra = @event.ContextExtra;
        });

        var contextExtra = new Dictionary<string, object>
        {
            { "perRequestContextExtraKey", "value" }
        };
        var res = await _plugin.Do(_token, new MessageActivity("hello world"), contextExtra);

        // staticContextExtraKey is registered with the app. And not passed in onEvent handlers.
        Assert.Equal(onEventExtra!["perRequestContextExtraKey"], "value");
        Assert.Equal(onActivityExtra!["perRequestContextExtraKey"], "value");
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