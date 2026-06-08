// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// A single state scope (conversation or user) — a string-keyed bag of values.
/// </summary>
/// <remarks>
/// Persisted scopes (conversation, user) are change-tracked with a dirty flag: <see cref="Set{T}"/>,
/// <see cref="Remove"/>, and <see cref="Clear"/> mark the scope dirty, so only mutated scopes are written
/// and a read-only turn writes nothing. Reads never mark a scope dirty; persist a mutation to a value
/// fetched via <see cref="Get{T}"/> by writing it back with <see cref="Set{T}"/>.
/// After the owning turn completes, every read and write throws <see cref="InvalidOperationException"/>
/// (see <see cref="TurnState"/>).
/// </remarks>
public sealed class StateScope
{
    private readonly Dictionary<string, object?> _values;
    private readonly bool _persisted;
    private bool _dirty;
    private bool _completed;

    internal StateScope(bool persisted, IReadOnlyDictionary<string, object?>? loaded)
    {
        _persisted = persisted;
        _values = loaded is not null ? new Dictionary<string, object?>(loaded) : [];
    }

    /// <summary>Gets the value stored under <paramref name="key"/>, or <c>default</c> if absent.</summary>
    /// <typeparam name="T">The value type to read.</typeparam>
    /// <param name="key">The value key.</param>
    public T? Get<T>(string key)
    {
        ThrowIfCompleted();
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (!_values.TryGetValue(key, out object? raw) || raw is null)
        {
            return default;
        }

        if (raw is T typed)
        {
            return typed;
        }

        if (raw is JsonElement element)
        {
            // Deserialize once per key per turn: cache the typed value for subsequent reads. Safe now
            // that change tracking is a dirty flag — a read doesn't dirty the scope, so this can't cause
            // a read-only turn to write. Persist a mutation to the returned value with Set.
            T? converted = StateSerializer.Convert<T>(element);
            _values[key] = converted;
            return converted;
        }

        return default;
    }

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
    /// <typeparam name="T">The value type to store.</typeparam>
    /// <param name="key">The value key.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string key, T value)
    {
        ThrowIfCompleted();
        ArgumentException.ThrowIfNullOrEmpty(key);
        _values[key] = value;
        _dirty = true;
    }

    /// <summary>Removes the value stored under <paramref name="key"/>.</summary>
    /// <param name="key">The value key.</param>
    /// <returns><see langword="true"/> if a value was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(string key)
    {
        ThrowIfCompleted();
        ArgumentException.ThrowIfNullOrEmpty(key);
        bool removed = _values.Remove(key);
        _dirty |= removed;
        return removed;
    }

    /// <summary>Whether a value is stored under <paramref name="key"/>.</summary>
    /// <param name="key">The value key.</param>
    public bool ContainsKey(string key)
    {
        ThrowIfCompleted();
        ArgumentException.ThrowIfNullOrEmpty(key);
        return _values.ContainsKey(key);
    }

    /// <summary>Removes every value from the scope. A persisted scope emptied this way is deleted from storage on save.</summary>
    public void Clear()
    {
        ThrowIfCompleted();
        _dirty |= _values.Count > 0;
        _values.Clear();
    }

    /// <summary>True if the scope currently holds no values.</summary>
    internal bool IsEmpty => _values.Count == 0;

    /// <summary>True if this is a persisted scope that was mutated this turn (Set / Remove / Clear).</summary>
    internal bool IsChanged() => _persisted && _dirty;

    /// <summary>Snapshots the scope's values into a new dictionary for writing to storage.</summary>
    internal Dictionary<string, object?> Snapshot() => new(_values);

    /// <summary>Seals the scope; subsequent access throws.</summary>
    internal void Complete() => _completed = true;

    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException(
                "TurnState was accessed after the turn completed. State is per-turn and is saved once " +
                "when the handler returns. Read the values you need during the turn and pass them into " +
                "any background work, e.g. `var name = ctx.State.User.Get<string>(\"name\");`.");
        }
    }
}
