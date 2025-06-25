using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class ActivityTests
{
    private readonly App _app;
    private readonly IToken _token = Globals.Token;
    private readonly TestPlugin _plugin = new();
    private readonly Controller _controller = new();

    private readonly IDictionary<string, object> _extra = new Dictionary<string, object>
    {
        { "staticContextExtraKey", "value" }
    };

    public ActivityTests()
    {
        _app = App.Builder().AddContextExtra(_extra).Build();
        _app.AddPlugin(_plugin);
        _app.AddController(_controller);
    }

    [Fact]
    public async Task Should_CallHandler_OnMessage()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessage);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
        Assert.Equal(1, _controller.Calls);
    }

    [Fact]
    public async Task Should_CallHandler_OnTyping()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsTyping);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
        Assert.Equal(1, _controller.Calls);
    }

    [Fact]
    public async Task Should_Pass_ContextExtra_OnActivity()
    {
        IDictionary<string, object>? extra = null;
        _app.OnActivity(context =>
        {
            extra = context.Extra;
            return Task.CompletedTask;
        });

        var contextExtra = new Dictionary<string, object>
        {
            { "perRequestContextExtraKey", "value" }
        };
        this._plugin.ContextExtra = contextExtra;
        var res = await _app.Process<TestPlugin>(_token, new MessageActivity());

        Assert.Equal(extra!["staticContextExtraKey"], "value");
        Assert.Equal(extra["perRequestContextExtraKey"], "value");
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Activity]
        public void OnActivity([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}