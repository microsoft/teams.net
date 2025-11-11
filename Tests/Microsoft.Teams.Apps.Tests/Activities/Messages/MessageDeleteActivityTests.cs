using Microsoft.Extensions.Logging;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

using Moq;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class MessageDeleteActivityTests
{
    private readonly Mock<ILogger<App>> _logger = new();
    private readonly App _app;
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public MessageDeleteActivityTests()
    {
        _app = new App(_logger.Object);
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
            Assert.True(context.Activity.Type.IsMessageDelete);
            return context.Next();
        });

        _app.OnMessageDelete(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageDelete);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageDeleteActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(3, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnMessageDelete(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageDelete);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Message.Delete]
        public void OnMessageDelete([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}