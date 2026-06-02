// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Globalization;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// In-process <see cref="IStorage"/> backed by a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Documents are stored as serialized JSON so reads behave like a real backend (values come back as
/// <see cref="System.Text.Json.JsonElement"/>). State is lost when the process restarts — intended
/// for development and testing.
/// </summary>
public sealed class MemoryStorage : IStorage
{
    private readonly ConcurrentDictionary<string, Entry> _store = new(StringComparer.Ordinal);
    private long _etag;

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, StoreItem>> ReadAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        Dictionary<string, StoreItem> result = [];
        foreach (string key in keys)
        {
            if (_store.TryGetValue(key, out Entry entry))
            {
                result[key] = new StoreItem
                {
                    Values = StateSerializer.Deserialize(entry.Json),
                    ETag = entry.ETag,
                };
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, StoreItem>>(result);
    }

    /// <inheritdoc />
    public Task WriteAsync(IReadOnlyDictionary<string, StoreItem> changes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(changes);

        foreach ((string key, StoreItem item) in changes)
        {
            string etag = Interlocked.Increment(ref _etag).ToString(CultureInfo.InvariantCulture);
            _store[key] = new Entry(StateSerializer.Serialize(item.Values), etag);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (string key in keys)
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private readonly record struct Entry(string Json, string ETag);
}
