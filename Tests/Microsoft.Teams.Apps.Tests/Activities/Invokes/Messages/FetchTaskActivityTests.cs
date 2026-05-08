// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Testing.Plugins;

using TaskModules = Microsoft.Teams.Api.TaskModules;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class FetchTaskActivityTests
{
    private readonly App _app = new();
    private readonly IToken _token = Globals.Token;
    private readonly Controller _controller = new();

    public FetchTaskActivityTests()
    {
        _app.AddPlugin(new TestPlugin());
        _app.AddController(_controller);
    }

    private static Messages.FetchTaskActivity SetupFetchTaskActivity(string reaction = "like")
    {
        return new Messages.FetchTaskActivity
        {
            Value = new Messages.FetchTaskActivity.FetchTaskValue
            {
                Data = new Messages.FetchTaskActivity.FetchTaskData
                {
                    ActionValue = new Messages.FetchTaskActivity.FetchTaskActionValue
                    {
                        Reaction = new Reaction(reaction),
                    },
                },
            },
        };
    }

    [Fact]
    public async Task Should_CallHandler()
    {
        var calls = 0;

        _app.OnMessageFetchTask((context, ct) =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(context.Activity.Name == Name.Messages.FetchTask);
            Assert.True(context.Activity.Value.Data.ActionValue.Reaction.IsLike);
            return Task.FromResult(new TaskModules.Response(new TaskModules.ContinueTask(new TaskModules.TaskInfo { Title = "Feedback" })));
        });

        var res = await _app.Process<TestPlugin>(_token, SetupFetchTaskActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
        Assert.Equal(1, _controller.Calls);
        Assert.Equal(2, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler_OnOtherActivity()
    {
        var calls = 0;

        _app.OnMessageFetchTask((context, ct) =>
        {
            calls++;
            return Task.FromResult(new TaskModules.Response(new TaskModules.ContinueTask(new TaskModules.TaskInfo())));
        });

        var res = await _app.Process<TestPlugin>(_token, new TypingActivity());

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
        Assert.Equal(0, _controller.Calls);
        Assert.Equal(0, res.Meta.Routes);
    }

    [Fact]
    public async Task Should_Not_CallHandler_OnSubmitAction()
    {
        var calls = 0;

        _app.OnMessageFetchTask((context, ct) =>
        {
            calls++;
            return Task.FromResult(new TaskModules.Response(new TaskModules.ContinueTask(new TaskModules.TaskInfo())));
        });

        var submit = new Messages.SubmitActionActivity
        {
            Value = new Messages.SubmitActionActivity.SubmitActionValue
            {
                ActionName = "feedback",
                ActionValue = "test",
            },
        };

        var res = await _app.Process<TestPlugin>(_token, submit);

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Should_CallHandler_OnDislikeReaction()
    {
        var calls = 0;

        _app.OnMessageFetchTask((context, ct) =>
        {
            calls++;
            Assert.True(context.Activity.Value.Data.ActionValue.Reaction.IsDislike);
            return Task.FromResult(new TaskModules.Response(new TaskModules.ContinueTask(new TaskModules.TaskInfo())));
        });

        var res = await _app.Process<TestPlugin>(_token, SetupFetchTaskActivity("dislike"));

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
    }

    [TeamsController]
    public class Controller
    {
        public int Calls { get; private set; }

        [Microsoft.Teams.Apps.Activities.Invokes.Message.FetchTask]
        public void OnFetchTask([Context] IContext.Next next)
        {
            Calls++;
            next();
        }
    }
}

