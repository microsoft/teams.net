// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable CA1716 // Get/Set are the idiomatic names for state access, matching ISession conventions.

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Provides per-turn state storage with key-value and typed object access.
/// </summary>
public interface ITurnState
{
    /// <summary>
    /// Gets a value by key. Returns default if the key is not present or the value cannot be converted.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a value by key.
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes a key from state.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Attempts to get a value by key.
    /// Returns true if the key exists and the value can be converted to <typeparamref name="T"/>.
    /// </summary>
    bool TryGet<T>(string key, out T? value);

    /// <summary>
    /// Returns true if the key exists in state.
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// Gets a typed state object. Creates a new instance via parameterless constructor if not present.
    /// </summary>
    T Get<T>() where T : class, new();

    /// <summary>
    /// Sets a typed state object, replacing any existing instance of the same type.
    /// </summary>
    void Set<T>(T value) where T : class;

    /// <summary>
    /// Returns true if a typed state object of this type exists.
    /// </summary>
    bool Has<T>() where T : class;

    /// <summary>
    /// Removes the typed state object of this type.
    /// </summary>
    void Remove<T>() where T : class;

    /// <summary>
    /// Returns true if any value has been added, modified, or removed since the state was loaded.
    /// </summary>
    bool IsDirty { get; }
}
