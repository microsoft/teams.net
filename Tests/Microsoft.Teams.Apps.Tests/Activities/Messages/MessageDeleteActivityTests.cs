using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

using static Microsoft.Teams.Apps.Activities.Message;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class MessageDeleteActivityTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public MessageDeleteActivityTests()
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
    }


    [Fact]
    public void MessageDeleteAttribute_ShouldHaveCorrectNameAndType()
    {
        // Arrange & Act
        var attribute = new DeleteAttribute();

        // Assert
        Assert.NotNull(attribute.Name);
        Assert.Equal(ActivityType.MessageDelete.ToString(), attribute.Name);
        Assert.Equal(typeof(MessageDeleteActivity), attribute.Type);
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