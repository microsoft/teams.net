namespace Microsoft.Teams.Common.Storage;

/// <summary>
/// a storage container that can get/set/delete items by a unique key
/// </summary>
/// <typeparam name="TKey">the key type</typeparam>
/// <typeparam name="TValue">the value type</typeparam>
public interface IStorage<TKey, TValue> where TKey : notnull
{
    public bool Exists(TKey key);
    public Task<bool> ExistsAsync(TKey key);

    public TValue? Get(TKey key);
    public T? Get<T>(TKey key) where T : TValue;
    public Task<TValue?> GetAsync(TKey key);
    public Task<T?> GetAsync<T>(TKey key) where T : TValue;

    public void Set(TKey key, TValue value);
    public Task SetAsync(TKey key, TValue value);

    public void Delete(TKey key);
    public Task DeleteAsync(TKey key);
}