// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Apps.Testing.Events;

namespace Microsoft.Teams.Apps.Testing.Plugins;

/// <summary>
/// a plugin used to test any App implementation
/// </summary>
[Plugin(Name = "test")]
public partial class TestPlugin : ISenderPlugin
{
    public event EventFunction Events;

    protected Action<App>? OnInitHandler { get; set; }
    protected Action<App>? OnStartHandler { get; set; }
    protected Action<App, IPlugin, ErrorEvent>? OnErrorHandler { get; set; }
    protected Action<App, ISenderPlugin, ActivityEvent>? OnActivityHandler { get; set; }
    protected Action<App, ISenderPlugin, ActivityResponseEvent>? OnActivityResponseHandler { get; set; }
    protected Action<App, ISenderPlugin, ActivitySentEvent>? OnActivitySentHandler { get; set; }

    public TestPlugin WithInit(Action<App> handler)
    {
        OnInitHandler = handler;
        return this;
    }

    public TestPlugin WithError(Action<App, IPlugin, ErrorEvent> handler)
    {
        OnErrorHandler = handler;
        return this;
    }

    public TestPlugin WithActivity(Action<App, ISenderPlugin, ActivityEvent> handler)
    {
        OnActivityHandler = handler;
        return this;
    }

    public TestPlugin WithActivityResponse(Action<App, ISenderPlugin, ActivityResponseEvent> handler)
    {
        OnActivityResponseHandler = handler;
        return this;
    }

    public TestPlugin WithActivitySent(Action<App, ISenderPlugin, ActivitySentEvent> handler)
    {
        OnActivitySentHandler = handler;
        return this;
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        if (OnInitHandler is not null)
        {
            OnInitHandler(app);
        }

        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        if (OnStartHandler is not null)
        {
            OnStartHandler(app);
        }

        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        if (OnErrorHandler is not null)
        {
            OnErrorHandler(app, plugin, @event);
        }

        return Task.CompletedTask;
    }

    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        if (OnActivityHandler is not null)
        {
            OnActivityHandler(app, sender, @event);
        }

        return Task.CompletedTask;
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        if (OnActivityResponseHandler is not null)
        {
            OnActivityResponseHandler(app, sender, @event);
        }

        return Task.CompletedTask;
    }

    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        if (OnActivitySentHandler is not null)
        {
            OnActivitySentHandler(app, sender, @event);
        }

        return Task.CompletedTask;
    }

    public Task<IActivity> Send(IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(activity);
    }

    public Task<TActivity> Send<TActivity>(TActivity activity, ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        return Task.FromResult(activity);
    }

    public IStreamer CreateStream(ConversationReference reference, CancellationToken cancellationToken = default)
    {
        return new Stream();
    }

    public async Task<Response> Do(IToken token, IActivity activity, IDictionary<string, object>? contextExtra = null, CancellationToken cancellationToken = default)
    {
        if (activity is MessageActivity message)
        {
            await Events(
                this,
                "message",
                new TestMessageEvent() { Message = message.Text },
                cancellationToken
            );
        }

        var @out = await Events(
            this,
            "activity",
            new ActivityEvent()
            {
                Token = token,
                Activity = activity,
                ContextExtra = contextExtra
            },
            cancellationToken
        );

        var res = (Response?)@out;

        if (res is null)
        {
            throw new Exception("expected activity response");
        }

        return res;
    }
}