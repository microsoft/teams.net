namespace Microsoft.Teams.Apps;

/// <summary>
/// any singleton registry
/// </summary>
internal interface IContainer
{
    /// <summary>
    /// does the container have the provided key
    /// </summary>
    public bool Has(string key);

    /// <summary>
    /// register a singleton
    /// </summary>
    public void Register(string key, IProvider provider);

    /// <summary>
    /// register a singleton
    /// </summary>
    /// <typeparam name="T">the type</typeparam>
    /// <param name="provider">the provider</param>
    public void Register<T>(IProvider provider) where T : notnull;

    /// <summary>
    /// register a singleton
    /// </summary>
    public void Register<T>(T value) where T : notnull;

    /// <summary>
    /// resolve a singleton
    /// </summary>
    public T? Resolve<T>(string key);
}

/// <summary>
/// any singleton registry
/// </summary>
internal class Container : IContainer
{
    protected Dictionary<string, object> _values = [];
    protected Dictionary<string, IProvider> _providers = [];

    public bool Has(string key)
    {
        return _providers.ContainsKey(key);
    }

    public void Register(string key, IProvider provider)
    {
        if (Has(key))
        {
            throw new InvalidOperationException($"key '{key}' already exists");
        }

        _providers.Add(key, provider);
    }

    public void Register<T>(IProvider provider) where T : notnull
    {
        var key = typeof(T).Name;

        if (Has(key))
        {
            throw new InvalidOperationException($"key '{key}' already exists");
        }

        _providers.Add(key, provider);
    }

    public void Register<T>(T value) where T : notnull
    {
        var key = typeof(T).Name;

        if (Has(key))
        {
            throw new InvalidOperationException($"key '{key}' already exists");
        }

        _providers.Add(key, new ValueProvider(value));
    }

    public T? Resolve<T>(string key)
    {
        var value = _values.TryGetValue(key, out var v) ? v : null;

        if (value is not null)
        {
            return (T)value;
        }

        var provider = _providers.TryGetValue(key, out var p) ? p : null;

        if (provider is null)
        {
            return default;
        }

        value = provider.Resolve();

        if (value is null)
        {
            return default;
        }

        _values.Add(key, value);
        return (T)value;
    }
}