using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Testing.Plugins;

namespace Microsoft.Teams.Apps.Tests.Concurrency;

public class AsynTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly TestPlugin _plugin = new();

    public AsynTests()
    {
        _app.AddPlugin(_plugin);
    }

    [Fact]
    public async Task Should_Have_Unique_Data()
    {
        _app.OnSubmitAction(context =>
        {
            var index = (int)context.Extra["index"];
            return Task.FromResult<object?>(index);
        });

        var tasks = new List<Task<Response>>();

        for (var i = 0; i < 200; i++)
        {
            tasks.Add(_plugin.Do(
                _token,
                new Messages.SubmitActionActivity()
                {
                    Value = new()
                    {
                        ActionName = "name",
                        ActionValue = "value"
                    }
                },
                new Dictionary<string, object>()
                {
                    { "index", i }
                }
            ));
        }

        var responses = await Task.WhenAll(tasks.ToArray());

        for (var i = 0; i < 200; i++)
        {
            Assert.Equal(i, responses[i].Body);
        }
    }
}