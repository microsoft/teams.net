using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Routing;

public class TypingActivityTests
{
    private readonly App _app;
    private readonly IToken _token;

    public TypingActivityTests()
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
            Assert.True(context.Activity.Type.IsTyping);
            return context.Next();
        });

        _app.OnTyping(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsTyping);
            Assert.Equal("testing123", context.Activity.Text);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity("testing123"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnTyping(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsTyping);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
    }
}