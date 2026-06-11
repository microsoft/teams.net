// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Per-turn state storage backed by a dictionary, supporting both key-value and typed object access.
/// This type is not thread-safe; each instance is scoped to a single turn.
/// </summary>
public class TurnState
{
    private readonly Dictionary<string, object?> _data;

    /// <summary>
    /// Initializes a new, empty <see cref="TurnState"/>.
    /// </summary>
    public TurnState() => _data = [];

    private TurnState(Dictionary<string, object?> data) => _data = data;

    /// <summary>
    /// Returns true if any value has been added, modified, or removed since the state was loaded.
    /// </summary>
    public bool IsDirty { get; internal set; }

    /// <summary>
    /// Gets a value by key. Returns <c>default</c> if the key is not present or the value cannot be converted.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (!_data.TryGetValue(key, out object? value) || value is null)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        // Handle JsonElement values from deserialization.
        if (value is JsonElement element)
        {
            try
            {
                return element.Deserialize<T>();
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                return default;
            }
        }

        return default;
    }

    /// <summary>
    /// Sets a value by key.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        _data[key] = value;
        IsDirty = true;
    }

    /// <summary>
    /// Removes a key from state.
    /// </summary>
    public void Remove(string key)
    {
        if (_data.Remove(key))
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// Attempts to get a value by key.
    /// Returns <c>true</c> if the key exists and the value can be converted to <typeparamref name="T"/>.
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        if (!_data.TryGetValue(key, out object? raw) || raw is null)
        {
            value = default;
            return false;
        }

        if (raw is T typed)
        {
            value = typed;
            return true;
        }

        if (raw is JsonElement element)
        {
            try
            {
                value = element.Deserialize<T>();
                return value is not null;
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                value = default;
                return false;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the key exists in state.
    /// </summary>
    public bool ContainsKey(string key) => _data.ContainsKey(key);

    /// <summary>
    /// Gets a typed state object. Creates a new instance via parameterless constructor if not present.
    /// </summary>
    public T Get<T>() where T : class, new()
    {
        string key = TypeKey<T>();

        if (_data.TryGetValue(key, out object? value) && value is not null)
        {
            if (value is T typed)
            {
                return typed;
            }

            if (value is JsonElement element)
            {
                try
                {
                    T deserialized = element.Deserialize<T>() ?? new T();
                    _data[key] = deserialized;
                    IsDirty = true;
                    return deserialized;
                }
                catch (Exception ex) when (ex is JsonException or NotSupportedException)
                {
                    // Fall through to create new instance
                }
            }
        }

        T instance = new();
        _data[key] = instance;
        IsDirty = true;
        return instance;
    }

    /// <summary>
    /// Sets a typed state object, replacing any existing instance of the same type.
    /// </summary>
    public void Set<T>(T value) where T : class
    {
        _data[TypeKey<T>()] = value;
        IsDirty = true;
    }

    /// <summary>
    /// Returns <c>true</c> if a typed state object of this type exists.
    /// </summary>
    public bool Has<T>() where T : class => _data.ContainsKey(TypeKey<T>());

    /// <summary>
    /// Removes the typed state object of this type.
    /// </summary>
    public void Remove<T>() where T : class
    {
        if (_data.Remove(TypeKey<T>()))
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// Serializes the state to a JSON byte array.
    /// </summary>
    public byte[] ToJsonBytes() => JsonSerializer.SerializeToUtf8Bytes(_data);

    /// <summary>
    /// Deserializes a <see cref="TurnState"/> from a JSON byte array.
    /// Returns an empty, non-dirty state when <paramref name="bytes"/> is null or empty.
    /// </summary>
    public static TurnState FromJsonBytes(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return new TurnState();
        }

        try
        {
            Dictionary<string, object?>? data = JsonSerializer.Deserialize<Dictionary<string, object?>>(bytes);
            return new TurnState(data ?? []);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Treat corrupted cache payload as a cache miss.
            return new TurnState();
        }
    }

    /// <summary>
    /// Creates a <see cref="TurnState"/> from an existing dictionary. Useful for testing.
    /// </summary>
    public static TurnState FromDictionary(Dictionary<string, object?> data)
    {
        return new TurnState(new Dictionary<string, object?>(data));
    }

    private static string TypeKey<T>() => $"${typeof(T).FullName}";
}
