using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Messages;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class MessageReactionActivityTests
{
    private readonly App _app;
    private readonly IToken _token;

    public MessageReactionActivityTests()
    {
        _app = new App();
        _app.AddPlugin(new TestPlugin());
        _token = Globals.Token;
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
            Assert.True(context.Activity.ReactionsAdded!.First().Type.IsAngry);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageReactionActivity().AddReaction(ReactionType.Angry));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
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
    }
}