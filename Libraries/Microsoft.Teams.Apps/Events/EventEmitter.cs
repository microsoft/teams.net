using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

internal class EventEmitter
{
    protected Dictionary<string, Topic> Topics { get; set; } = [];

    public EventEmitter On(string name, Action<IPlugin, Event> handler)
    {
        var topic = Topics.TryGetValue(name, out Topic? value) ? value : [];

        topic.Add(delegate (IPlugin plugin, Event @event, CancellationToken cancellationToken)
        {
            handler(plugin, @event);
            return Task.FromResult<object?>(null);
        });

        Topics[name] = topic;
        return this;
    }

    public EventEmitter On<TResult>(string name, Func<IPlugin, Event, TResult> handler)
    {
        var topic = Topics.TryGetValue(name, out Topic? value) ? value : [];

        topic.Add(delegate (IPlugin plugin, Event @event, CancellationToken cancellationToken)
        {
            var res = handler(plugin, @event);
            return Task.FromResult<object?>(res);
        });

        Topics[name] = topic;
        return this;
    }

    public EventEmitter On(string name, Func<IPlugin, Event, CancellationToken, Task> handler)
    {
        var topic = Topics.TryGetValue(name, out Topic? value) ? value : [];

        topic.Add(async delegate (IPlugin plugin, Event @event, CancellationToken cancellationToken)
        {
            await handler(plugin, @event, cancellationToken);
            return null;
        });

        Topics[name] = topic;
        return this;
    }

    public EventEmitter On<TResult>(string name, Func<IPlugin, Event, CancellationToken, Task<TResult>> handler)
    {
        var topic = Topics.TryGetValue(name, out Topic? value) ? value : [];

        topic.Add(async delegate (IPlugin plugin, Event @event, CancellationToken cancellationToken)
        {
            var res = await handler(plugin, @event, cancellationToken);
            return res;
        });

        Topics[name] = topic;
        return this;
    }

    public Task<object?> Emit(IPlugin plugin, string name, Event? @event = null, CancellationToken cancellationToken = default)
    {
        var topic = Topics.TryGetValue(name, out Topic? value) ? value : [];
        return topic.Emit(plugin, @event, cancellationToken);
    }
}