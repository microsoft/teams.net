using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class InstallActivityTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public InstallActivityTests()
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
            Assert.True(context.Activity.Type.IsInstallUpdate);
            return context.Next();
        });

        _app.OnInstall(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInstallUpdate);
            Assert.Equal(InstallUpdateAction.Add, context.Activity.Action);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new InstallUpdateActivity()
        {
            Action = InstallUpdateAction.Add
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnInstall(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInstallUpdate);
            Assert.Equal(InstallUpdateAction.Add, context.Activity.Action);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new InstallUpdateActivity()
        {
            Action = InstallUpdateAction.Remove
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Install]
        public void OnInstall([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}