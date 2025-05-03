using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Routing;

public class MessageActivityTests
{
    private readonly IApp _app;
    private readonly IToken _token;

    public MessageActivityTests()
    {
        _app = new App();
        _app.AddPlugin(new TestPlugin());
        _token = new JsonWebToken("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30");
    }

    [Fact]
    public async Task Should_CallHandler()
    {
        var calls = 0;

        _app.OnMessage(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessage);
            Assert.Equal("testing123", context.Activity.Text);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new MessageActivity("testing123"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnMessage(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsMessage);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
    }
}