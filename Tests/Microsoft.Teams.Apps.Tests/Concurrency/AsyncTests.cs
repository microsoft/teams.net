using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Concurrency;

public class AsynTests
{
    private readonly App _app;
    private readonly IToken _token = Globals.Token;
    private readonly TestPlugin _plugin = new();
    private readonly IServiceProvider _provider;

    public AsynTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IContext.Accessor>();
        services.AddSingleton<Controller>();

        _provider = services.BuildServiceProvider();
        _app = new App(NullLogger<App>.Instance);
        _app.AddController(_provider.GetRequiredService<Controller>());
        _app.AddPlugin(_plugin);
    }

    [Fact]
    public async Task Should_Have_Unique_Data()
    {
        var tasks = new List<Task<Response>>();

        for (var i = 0; i < 200; i++)
        {
            tasks.Add(_plugin.Do(
                new()
                {
                    Token = _token,
                    Activity = new Messages.SubmitActionActivity()
                    {
                        Value = new()
                        {
                            ActionName = "name",
                            ActionValue = "value"
                        }
                    },
                    Extra = new Dictionary<string, object?>()
                    {
                        { "index", i }
                    },
                    Services = _provider.CreateScope().ServiceProvider
                }
            ));
        }

        var responses = await Task.WhenAll(tasks.ToArray());

        for (var i = 0; i < 200; i++)
        {
            Assert.Equal(i, responses[i].Body);
        }
    }

    [TeamsController]
    public class Controller(IContext.Accessor accessor)
    {
        [Message.SubmitAction]
        public Task<object?> OnSubmitAction(IContext<Messages.SubmitActionActivity> context)
        {
            Assert.NotNull(accessor.Value);
            Assert.Equal(accessor.Value.Extra["index"], context.Extra["index"]);
            return Task.FromResult(context.Extra["index"]);
        }
    }
}