using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class MessageUpdateActivityTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public MessageUpdateActivityTests()
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
            Assert.True(context.Activity.Type.IsMessageUpdate);
            return context.Next();
        });

        _app.OnMessageUpdate(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageUpdate);
            Assert.Equal("testing123", context.Activity.Text);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageUpdateActivity("testing123"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnMessageUpdate(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageUpdate);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Message.Update]
        public void OnMessageUpdate([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}