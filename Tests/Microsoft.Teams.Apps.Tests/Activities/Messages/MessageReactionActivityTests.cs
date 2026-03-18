using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class MessageReactionActivityTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public MessageReactionActivityTests()
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
            Assert.True(context.Activity.Type.IsMessageReaction);
            return context.Next();
        });

        _app.OnMessageReaction(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            Assert.Single(context.Activity.ReactionsAdded ?? []);
            Assert.True(context.Activity.ReactionsAdded!.First().Type.IsLike);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageReactionActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(3, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnMessageReaction(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_CallHandler_OnAdd()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            return context.Next();
        });

        _app.OnMessageReaction(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            Assert.Single(context.Activity.ReactionsAdded ?? []);
            Assert.True(context.Activity.ReactionsAdded!.First().Type.IsLike);
            return context.Next();
        });

        _app.OnMessageReactionAdded(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            Assert.Single(context.Activity.ReactionsAdded ?? []);
            Assert.Empty(context.Activity.ReactionsRemoved ?? []);
            Assert.True(context.Activity.ReactionsAdded!.First().Type.IsLike);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageReactionActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(3, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_CallHandler_OnRemove()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            return context.Next();
        });

        _app.OnMessageReaction(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            Assert.Single(context.Activity.ReactionsRemoved ?? []);
            Assert.True(context.Activity.ReactionsRemoved!.First().Type.IsLike);
            return context.Next();
        });

        _app.OnMessageReactionRemoved(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessageReaction);
            Assert.Single(context.Activity.ReactionsAdded ?? []);
            Assert.Empty(context.Activity.ReactionsRemoved ?? []);
            Assert.True(context.Activity.ReactionsRemoved!.First().Type.IsLike);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageReactionActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(3, res.Meta.Routes);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Message.Reaction]
        public void OnMessageReaction([Context] IContext.Next next)
        {
            Calls++;
            next();
        }

        [Message.ReactionAdded]
        public void OnMessageReactionAdded([Context] IContext.Next next)
        {
            Calls++;
            next();
        }

        [Message.ReactionRemoved]
        public void OnMessageReactionRemoved([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}