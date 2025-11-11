// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

using Moq;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class FeedbackActivityTests
{
    private readonly Mock<ILogger<App>> _logger = new();
    private readonly App _app;
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public FeedbackActivityTests()
    {
        _app = new App(_logger.Object);
        _app.AddPlugin(new TestPlugin());
        _app.AddController(_controller);
    }

    private Messages.SubmitActionActivity SetupFeedbackActivity(string actionName = "feedback", string actionValue = "test")
    {
        return new Messages.SubmitActionActivity()
        {
            Value = new Messages.SubmitActionActivity.SubmitActionValue()
            {
                ActionName = actionName,
                ActionValue = actionValue
            }
        };
    }

    [Fact]
    public async Task Should_CallHandler()
    {
        var calls = 0;

        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name == Name.Messages.SubmitAction);
            return context.Next();
        });

        _app.OnFeedback(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Messages.SubmitAction);
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, SetupFeedbackActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(3, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler()
    {
        var calls = 0;

        _app.OnFeedback(context =>
        {
            calls++;
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler_WhenWrongAction()
    {
        var calls = 0;

        _app.OnFeedback(context =>
        {
            calls++;
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, SetupFeedbackActivity("other_action"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler_WhenValueNull()
    {
        var calls = 0;

        _app.OnFeedback(context =>
        {
            calls++;
            return Task.CompletedTask;
        });

        var res = await _app.Process<TestPlugin>(_token, new Messages.SubmitActionActivity()
        {
            Value = null!
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; } = 0;

        [Microsoft.Teams.Apps.Activities.Invokes.Message.Feedback]
        public void OnFeedback([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}