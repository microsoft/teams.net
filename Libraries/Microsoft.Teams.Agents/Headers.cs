using System.Collections;

namespace Microsoft.Teams.Agents;

public interface IHeaders : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
{
    public int Count { get; }

    public bool Has(params string[] keys);
    public IEnumerable<string> Get(string key);
    public IHeaders Add(string key, params string[] value);
    public IHeaders Remove(params string[] keys);
}

public class HeaderCollection : IHeaders
{
    public int Count => _store.Count;

    protected IDictionary<string, IEnumerable<string>> _store;

    public HeaderCollection()
    {
        _store = new Dictionary<string, IEnumerable<string>>();
    }

    public bool Has(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!_store.ContainsKey(key))
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerable<string> Get(string key)
    {
        if (!_store.ContainsKey(key)) return [];
        return _store[key];
    }

    public IHeaders Add(string key, params string[] value)
    {
        _store[key] = value;
        return this;
    }

    public IHeaders Remove(params string[] keys)
    {
        foreach (var key in keys)
        {
            _store.Remove(key);
        }

        return this;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator() => _store.GetEnumerator();
}