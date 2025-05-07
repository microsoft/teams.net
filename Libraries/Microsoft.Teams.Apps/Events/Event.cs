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
}