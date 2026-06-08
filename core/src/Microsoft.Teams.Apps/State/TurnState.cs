// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Default implementation of <see cref="ITurnState"/> backed by a dictionary.
/// </summary>
public class TurnState : ITurnState
{
    private readonly Dictionary<string, object?> _data;

    /// <summary>
    /// Initializes a new, empty <see cref="TurnState"/>.
    /// </summary>
    public TurnState() => _data = [];

    private TurnState(Dictionary<string, object?> data) => _data = data;

    /// <inheritdoc/>
    public bool IsDirty { get; private set; }

    /// <inheritdoc/>
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
            return element.Deserialize<T>();
        }

        return default;
    }

    /// <inheritdoc/>
    public void Set<T>(string key, T value)
    {
        _data[key] = value;
        IsDirty = true;
    }

    /// <inheritdoc/>
    public void Remove(string key)
    {
        if (_data.Remove(key))
        {
            IsDirty = true;
        }
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _data.ContainsKey(key);

    /// <inheritdoc/>
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
                T deserialized = element.Deserialize<T>() ?? new T();
                _data[key] = deserialized;
                return deserialized;
            }
        }

        T instance = new();
        _data[key] = instance;
        IsDirty = true;
        return instance;
    }

    /// <inheritdoc/>
    public void Set<T>(T value) where T : class
    {
        _data[TypeKey<T>()] = value;
        IsDirty = true;
    }

    /// <inheritdoc/>
    public bool Has<T>() where T : class => _data.ContainsKey(TypeKey<T>());

    /// <inheritdoc/>
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

        Dictionary<string, object?>? data = JsonSerializer.Deserialize<Dictionary<string, object?>>(bytes);
        return new TurnState(data ?? []);
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
