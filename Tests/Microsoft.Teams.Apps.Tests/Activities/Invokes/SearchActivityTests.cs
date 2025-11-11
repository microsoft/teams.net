using Microsoft.Extensions.Logging;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Search;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

using Moq;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class SearchActivityTests
{
    private readonly Mock<ILogger<App>> _logger = new();

    private readonly App _app;
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public SearchActivityTests()
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
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name.IsSearch);
            return context.Next();
        });

        _app.OnSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Search);
            Assert.True(context.Activity.Value.Kind == SearchType.Search);
            return Task.FromResult<object?>(null);
        });

        var res = await _app.Process<TestPlugin>(_token, new SearchActivity()
        {
            Value = new()
            {
                Kind = SearchType.Search,
                QueryText = "test"
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(3, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_CallHandler_Answer()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name.IsSearch);
            return context.Next();
        });

        _app.OnSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Search);
            Assert.True(context.Activity.Value.Kind == SearchType.SearchAnswer);
            return context.Next();
        });

        _app.OnAnswerSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Search);
            Assert.True(context.Activity.Value.Kind == SearchType.SearchAnswer);
            return Task.FromResult<object?>(null);
        });

        var res = await _app.Process<TestPlugin>(_token, new SearchActivity()
        {
            Value = new()
            {
                Kind = SearchType.SearchAnswer,
                QueryText = "test"
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(3, calls);
        Assert.Equal(2, _controller.Calls);
        Assert.Equal(5, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_CallHandler_Typeahead()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name.IsSearch);
            return context.Next();
        });

        _app.OnSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Search);
            Assert.True(context.Activity.Value.Kind == SearchType.Typeahead);
            return context.Next();
        });

        _app.OnTypeaheadSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Search);
            Assert.True(context.Activity.Value.Kind == SearchType.Typeahead);
            return Task.FromResult<object?>(null);
        });

        var res = await _app.Process<TestPlugin>(_token, new SearchActivity()
        {
            Value = new()
            {
                Kind = SearchType.Typeahead,
                QueryText = "test"
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(3, calls);
        Assert.Equal(2, _controller.Calls);
        Assert.Equal(5, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            return Task.FromResult<object?>(null);
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler_Answer()
    {
        var calls = 0;

        _app.OnSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            return context.Next();
        });

        _app.OnTypeaheadSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            return context.Next();
        });

        _app.OnAnswerSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            return context.Next();
        });

        var res = await _app.Process<TestPlugin>(_token, new SearchActivity()
        {
            Value = new()
            {
                Kind = SearchType.Typeahead,
                QueryText = "test"
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(2, _controller.Calls);
        Assert.Equal(4, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler_Typeahead()
    {
        var calls = 0;

        _app.OnSearch(async context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            await context.Next();
        });

        _app.OnTypeaheadSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            return context.Next();
        });

        _app.OnAnswerSearch(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name.IsSearch);
            return context.Next();
        });

        var res = await _app.Process<TestPlugin>(_token, new SearchActivity()
        {
            Value = new()
            {
                Kind = SearchType.SearchAnswer,
                QueryText = "test"
            }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(2, _controller.Calls);
        Assert.Equal(4, res.Meta.Routes);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Search]
        public void OnSearch([Context] IContext.Next next)
        {
            Calls++;
            next();
        }

        [Search.Answer]
        public void OnAnswerSearch([Context] IContext.Next next)
        {
            Calls++;
            next();
        }

        [Search.Typeahead]
        public void OnTypeaheadSearch([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}