
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Testing.Plugins;

/// <summary>
/// a plugin used to test any IApp implementation
/// </summary>
[Plugin]
public partial class TestPlugin : ISenderPlugin
{
    public event ISenderPlugin.ActivityEventHandler ActivityEvent;
    public event IPlugin.ErrorEventHandler ErrorEvent;

    protected Action<IApp>? OnInitHandler { get; set; }
    protected Action<IApp>? OnStartHandler { get; set; }
    protected Action<IApp, IPlugin?, Exception, IContext<IActivity>?>? OnErrorHandler { get; set; }
    protected Action<IApp, IContext<IActivity>>? OnActivityHandler { get; set; }
    protected Action<IApp, Response?, IContext<IActivity>>? OnActivityResponseHandler { get; set; }
    protected Action<IApp, IActivity, IContext<IActivity>>? OnActivitySentHandler { get; set; }

    public TestPlugin WithInit(Action<IApp> handler)
    {
        OnInitHandler = handler;
        return this;
    }

    public TestPlugin WithError(Action<IApp, IPlugin?, Exception, IContext<IActivity>?> handler)
    {
        OnErrorHandler = handler;
        return this;
    }

    public TestPlugin WithActivity(Action<IApp, IContext<IActivity>> handler)
    {
        OnActivityHandler = handler;
        return this;
    }

    public TestPlugin WithActivityResponse(Action<IApp, Response?, IContext<IActivity>> handler)
    {
        OnActivityResponseHandler = handler;
        return this;
    }

    public TestPlugin WithActivitySent(Action<IApp, IActivity, IContext<IActivity>> handler)
    {
        OnActivitySentHandler = handler;
        return this;
    }

    public Task OnInit(IApp app, CancellationToken cancellationToken = default)
    {
        if (OnInitHandler is not null)
        {
            OnInitHandler(app);
        }

        return Task.CompletedTask;
    }

    public Task OnStart(IApp app, CancellationToken cancellationToken = default)
    {
        if (OnStartHandler is not null)
        {
            OnStartHandler(app);
        }

        return Task.CompletedTask;
    }

    public Task OnError(IApp app, IPlugin? plugin, Exception exception, IContext<IActivity>? context, CancellationToken cancellationToken = default)
    {
        if (OnErrorHandler is not null)
        {
            OnErrorHandler(app, plugin, exception, context);
        }

        return Task.CompletedTask;
    }

    public Task OnActivity(IApp app, IContext<IActivity> context)
    {
        if (OnActivityHandler is not null)
        {
            OnActivityHandler(app, context);
        }

        return Task.CompletedTask;
    }

    public Task OnActivityResponse(IApp app, Response? response, IContext<IActivity> context)
    {
        if (OnActivityResponseHandler is not null)
        {
            OnActivityResponseHandler(app, response, context);
        }

        return Task.CompletedTask;
    }

    public Task OnActivitySent(IApp app, IActivity activity, IContext<IActivity> context)
    {
        if (OnActivitySentHandler is not null)
        {
            OnActivitySentHandler(app, activity, context);
        }

        return Task.CompletedTask;
    }

    public Task OnActivitySent(IApp app, ISenderPlugin sender, IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
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

    public Task<Response> Do(IToken token, IActivity activity, CancellationToken cancellationToken = default)
    {
        return ActivityEvent(this, token, activity, cancellationToken);
    }
}