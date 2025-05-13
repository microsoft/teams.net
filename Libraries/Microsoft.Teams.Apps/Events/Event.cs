using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

/// <summary>
/// the base Event payload type
/// </summary>
public class Event : Dictionary<string, object>
{
    public object? GetOrDefault(string key) => ContainsKey(key) ? this[key] : null;
    public T? GetOrDefault<T>(string key) => (T?)GetOrDefault(key);

    public object Get(string key) => this[key];
    public T Get<T>(string key) => (T)this[key];

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class EventAttribute(string name) : Attribute
{
    public readonly string Name = name;
}

public static partial class AppEventExtensions
{
    public static App OnEvent(this App app, string name, Action<IPlugin, Event> handler)
    {
        app.Events.On(name, handler);
        return app;
    }

    public static App OnEvent<TEvent>(this App app, string name, Action<IPlugin, TEvent> handler) where TEvent : Event
    {
        app.Events.On(name, (plugin, payload) => handler(plugin, (TEvent)payload));
        return app;
    }

    public static App OnEvent(this App app, string name, Func<IPlugin, Event, CancellationToken, Task> handler)
    {
        app.Events.On(name, handler);
        return app;
    }

    public static App OnEvent<TEvent>(this App app, string name, Func<IPlugin, TEvent, CancellationToken, Task> handler) where TEvent : Event
    {
        app.Events.On(name, (plugin, payload, token) => handler(plugin, (TEvent)payload, token));
        return app;
    }
}