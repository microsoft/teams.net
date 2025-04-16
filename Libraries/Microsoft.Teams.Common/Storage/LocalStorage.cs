namespace Microsoft.Teams.Common.Storage;

/// <summary>
/// a local in-memory `IStorage` implementation
/// </summary>
/// <typeparam name="TValue">the value type</typeparam>
public class LocalStorage<TValue> : IStorage<string, TValue>
{
    protected Dictionary<string, TValue> _store = [];
    protected IList<string> _keys = [];
    protected int? _max;

    /// <summary>
    /// the number of items in the storage
    /// </summary>
    public int Size => _store.Count;

    /// <summary>
    /// get the list of storage keys
    /// </summary>
    public IList<string> Keys => _store.Keys.ToList();

    public LocalStorage(int? max = null)
    {
        _max = max;
    }

    public LocalStorage(IDictionary<string, TValue> data, int? max)
    {
        _store = new Dictionary<string, TValue>(data);
        _keys = data.Keys.ToList();
        _max = max;
    }

    public bool Exists(string key) => _store.ContainsKey(key);
    public Task<bool> ExistsAsync(string key) => Task.FromResult(Exists(key));

    public TValue? Get(string key)
    {
        Hit(key);
        return _store.TryGetValue(key, out var value) ? value : default;
    }

    public T? Get<T>(string key) where T : TValue
    {
        var value = Get(key);
        return (T?)value;
    }

    public Task<TValue?> GetAsync(string key)
    {
        return Task.FromResult(Get(key));
    }

    public async Task<T?> GetAsync<T>(string key) where T : TValue
    {
        var value = await GetAsync(key);
        return (T?)value;
    }

    public void Set(string key, TValue value)
    {
        if (!Hit(key)) _keys.Add(key);
        if (_max != null)
        {
            if (_keys.Count > _max)
            {
                var toRemove = _keys.ElementAt(0);
                _keys.RemoveAt(0);
                _store.Remove(toRemove);
            }
        }

        _store[key] = value;
    }

    public Task SetAsync(string key, TValue value)
    {
        return Task.Run(() => Set(key, value));
    }

    public void Delete(string key)
    {
        var index = _keys.IndexOf(key);

        if (index == -1) return;

        _keys.RemoveAt(index);
        _store.Remove(key);
    }

    public Task DeleteAsync(string key)
    {
        return Task.Run(() => Delete(key));
    }

    protected bool Hit(string key)
    {
        if (!Exists(key)) return false;
        if (Keys.Last() == key) return true;

        var index = _keys.IndexOf(key);

        if (index < 0) return false;

        for (var i = index + 1; i < _keys.Count; i++)
        {
            var tmp = _keys[i - 1];
            _keys[i - 1] = _keys[i];
            _keys[i] = tmp;
        }

        return true;
    }
}