using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Apps;

public partial class App
{
    internal EventEmitter Events = new();

    public App OnEvent(string name, Action<IPlugin, Event> handler)
    {
        Events.On(name, handler);
        return this;
    }

    public App OnEvent<TEvent>(string name, Action<IPlugin, TEvent> handler) where TEvent : Event
    {
        Events.On(name, (plugin, payload) => handler(plugin, (TEvent)payload));
        return this;
    }

    public App OnEvent(string name, Func<IPlugin, Event, CancellationToken, Task> handler)
    {
        Events.On(name, handler);
        return this;
    }

    public App OnEvent<TEvent>(string name, Func<IPlugin, TEvent, CancellationToken, Task> handler) where TEvent : Event
    {
        Events.On(name, (plugin, payload, token) => handler(plugin, (TEvent)payload, token));
        return this;
    }

    public App OnError(Action<IPlugin, ErrorEvent> handler)
    {
        return OnEvent("error", handler);
    }

    public App OnError(Func<IPlugin, ErrorEvent, CancellationToken, Task> handler)
    {
        return OnEvent("error", handler);
    }

    public App OnStart(Action<IPlugin> handler)
    {
        return OnEvent("start", (plugin, _) => handler(plugin));
    }

    public App OnStart(Func<IPlugin, Task> handler)
    {
        return OnEvent("start", (plugin, _) => handler(plugin));
    }

    public App OnActivity(Action<ISenderPlugin, ActivityEvent> handler)
    {
        return OnEvent("activity", (plugin, @event) => handler((ISenderPlugin)plugin, (ActivityEvent)@event));
    }

    public App OnActivity(Func<ISenderPlugin, ActivityEvent, CancellationToken, Task> handler)
    {
        return OnEvent("activity", (plugin, @event, token) => handler((ISenderPlugin)plugin, (ActivityEvent)@event, token));
    }

    public App OnActivitySent(Action<ISenderPlugin, ActivitySentEvent> handler)
    {
        return OnEvent("activity.sent", (plugin, @event) => handler((ISenderPlugin)plugin, (ActivitySentEvent)@event));
    }

    public App OnActivitySent(Func<ISenderPlugin, ActivitySentEvent, CancellationToken, Task> handler)
    {
        return OnEvent("activity.sent", (plugin, @event, token) => handler((ISenderPlugin)plugin, (ActivitySentEvent)@event, token));
    }

    public App OnActivityResponse(Action<ISenderPlugin, ActivityResponseEvent> handler)
    {
        return OnEvent("activity.response", (plugin, @event) => handler((ISenderPlugin)plugin, (ActivityResponseEvent)@event));
    }

    public App OnActivityResponse(Func<ISenderPlugin, ActivityResponseEvent, CancellationToken, Task> handler)
    {
        return OnEvent("activity.response", (plugin, @event, token) => handler((ISenderPlugin)plugin, (ActivityResponseEvent)@event, token));
    }

    protected async Task OnErrorEvent(IPlugin sender, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken = @event.Context?.CancellationToken ?? cancellationToken;
        Logger.Error(@event.Exception);

        if (@event.Exception is HttpException ex)
        {
            Logger.Error(ex.Request?.RequestUri?.ToString());

            if (ex.Request?.Content is not null)
            {
                var content = await ex.Request.Content.ReadAsStringAsync();
                Logger.Error(content);
            }
        }

        foreach (var plugin in Plugins)
        {
            if (sender.Equals(plugin)) continue;
            await plugin.OnError(this, sender, @event, cancellationToken);
        }
    }

    protected async Task OnActivitySentEvent(ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        foreach (var plugin in Plugins)
        {
            if (sender.Equals(plugin)) continue;
            await plugin.OnActivitySent(this, sender, @event, cancellationToken);
        }
    }

    protected async Task OnActivityResponseEvent(ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        foreach (var plugin in Plugins)
        {
            if (sender.Equals(plugin)) continue;
            await plugin.OnActivityResponse(this, sender, @event, cancellationToken);
        }
    }

    protected Task<Response> OnActivityEvent(ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        return Process(sender, @event.Token, @event.Activity, cancellationToken);
    }
}