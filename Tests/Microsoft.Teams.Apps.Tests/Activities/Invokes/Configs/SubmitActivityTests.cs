using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class ConfigsSubmitActionActivityTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public ConfigsSubmitActionActivityTests()
    {
        _app.AddPlugin(new TestPlugin());
        _app.AddController(_controller);
    }

    [Fact]
    public async Task Should_CallHandler()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name.IsConfig);
            return context.Next();
        });

        _app.OnConfigSubmit(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Configs.Submit);
            return Task.FromResult<object?>(null);
        });

        var res = await _app.Process<TestPlugin>(_token, new Configs.SubmitActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnConfigSubmit(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsConfig);
            return Task.FromResult<object?>(null);
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Config.Fetch]
        public void OnFetch([Context] IContext.Next next)
        {
            Calls++;
            next();
        }

        [Config.Submit]
        public void OnSubmit([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}