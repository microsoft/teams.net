using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Apps;

public partial interface IApp
{
    public IApp OnError(ErrorEventHandler handler);
    public IApp OnStart(StartEventHandler handler);
    public IApp OnActivity(ActivityEventHandler handler);
    public IApp OnActivitySent(ActivitySentEventHandler handler);
    public IApp OnActivityResponse(ActivityResponseEventHandler handler);

    public delegate Task StartEventHandler(IApp app, ILogger logger);
    public delegate Task ErrorEventHandler(IApp app, IPlugin? plugin, Exception exception, IContext<IActivity>? context);
    public delegate Task ActivityEventHandler(IApp app, IContext<IActivity> context);
    public delegate Task ActivitySentEventHandler(IApp app, IActivity activity, IContext<IActivity> context);
    public delegate Task ActivityResponseEventHandler(IApp app, Response? response, IContext<IActivity> context);
}

public partial class App
{
    protected event IApp.ErrorEventHandler ErrorEvent;
    protected event IApp.StartEventHandler StartEvent;
    protected event IApp.ActivityEventHandler ActivityEvent;
    protected event IApp.ActivitySentEventHandler ActivitySentEvent;
    protected event IApp.ActivityResponseEventHandler ActivityResponseEvent;

    public IApp OnError(IApp.ErrorEventHandler handler)
    {
        ErrorEvent += handler;
        return this;
    }

    public IApp OnStart(IApp.StartEventHandler handler)
    {
        StartEvent += handler;
        return this;
    }

    public IApp OnActivity(IApp.ActivityEventHandler handler)
    {
        ActivityEvent += handler;
        return this;
    }

    public IApp OnActivitySent(IApp.ActivitySentEventHandler handler)
    {
        ActivitySentEvent += handler;
        return this;
    }

    public IApp OnActivityResponse(IApp.ActivityResponseEventHandler handler)
    {
        ActivityResponseEvent += handler;
        return this;
    }

    protected async Task OnErrorEvent(IPlugin? sender, Exception exception, IContext<IActivity>? context)
    {
        var cancellationToken = context?.CancellationToken ?? default;
        Logger.Error(exception);

        if (exception is HttpException ex)
        {
            Logger.Error(ex.Request?.RequestUri?.ToString());

            if (ex.Request?.Content != null)
            {
                var content = await ex.Request.Content.ReadAsStringAsync();
                Logger.Error(content);
            }
        }

        foreach (var plugin in Plugins)
        {
            if (sender != null && sender.Equals(plugin)) continue;
            await plugin.OnError(this, sender, exception, context, cancellationToken);
        }
    }

    protected Task OnStartEvent()
    {
        return Task.Run(() => Logger.Info("started"));
    }

    protected async Task OnActivitySentEvent(IActivity activity, IContext<IActivity> context)
    {
        Logger.Debug(activity);

        foreach (var plugin in Plugins)
        {
            await plugin.OnActivitySent(this, activity, context);
        }
    }

    protected async Task OnActivitySentEvent(ISenderPlugin sender, IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        Logger.Debug(activity);

        foreach (var plugin in Plugins)
        {
            await plugin.OnActivitySent(this, sender, activity, reference, cancellationToken);
        }
    }

    protected async Task OnActivityResponseEvent(Response? response, IContext<IActivity> context)
    {
        Logger.Debug(response);

        foreach (var plugin in Plugins)
        {
            await plugin.OnActivityResponse(this, response, context);
        }
    }

    protected Task<Response> OnActivityEvent(ISenderPlugin sender, IToken token, IActivity activity, CancellationToken cancellationToken = default)
    {
        return Process(sender, token, activity, cancellationToken); 
    }
}