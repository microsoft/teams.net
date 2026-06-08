// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Loads and saves <see cref="TurnState"/> documents in an <see cref="IDistributedCache"/>. The
/// orchestrator (<c>TeamsBotApplication.OnActivity</c>) loads state at the start of a turn, passes the
/// <see cref="TurnState"/> into the per-turn <c>Context</c>, and saves changed scopes when the turn
/// completes successfully (skipping the save on exception gives atomic save).
/// </summary>
/// <remarks>
/// Any <see cref="IDistributedCache"/> backend works — <c>AddDistributedMemoryCache</c> for in-process
/// dev, <c>AddStackExchangeRedisCache</c> / <c>AddDistributedSqlServerCache</c> for multi-instance.
/// Each scope document is stored as bare camelCase JSON (UTF-8); a backend that stores the value
/// verbatim keeps it cross-runtime readable, while the built-in <c>RedisCache</c> wraps it in a Redis
/// hash. Concurrency is last-write-wins. Pass <see cref="DistributedCacheEntryOptions"/> to apply a TTL.
/// </remarks>
public sealed class TurnStateStore
{
    private static readonly IReadOnlyDictionary<string, Dictionary<string, object?>> EmptyItems = new Dictionary<string, Dictionary<string, object?>>();

    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _entryOptions;

    /// <summary>Initializes a new <see cref="TurnStateStore"/> backed by the given distributed cache.</summary>
    /// <param name="cache">The distributed cache that persists state documents.</param>
    /// <param name="entryOptions">
    /// Optional per-entry options (e.g. absolute/sliding expiration) applied to every write. When null,
    /// entries are written without expiration.
    /// </param>
    internal TurnStateStore(IDistributedCache cache, DistributedCacheEntryOptions? entryOptions = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
        _entryOptions = entryOptions ?? new DistributedCacheEntryOptions();
    }

    /// <summary>
    /// Loads the conversation and user scopes for the given activity into a fresh <see cref="TurnState"/>.
    /// </summary>
    /// <param name="activity">The incoming activity. Only base routing fields are read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    internal async Task<TurnState> LoadAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);

        (string? conversationKey, string? userKey) = TurnState.DeriveKeys(activity);

        List<string> keys = [];
        if (conversationKey is not null)
        {
            keys.Add(conversationKey);
        }
        if (userKey is not null)
        {
            keys.Add(userKey);
        }

        IReadOnlyDictionary<string, Dictionary<string, object?>> items = keys.Count > 0
            ? await ReadAsync(keys, cancellationToken).ConfigureAwait(false)
            : EmptyItems;

        Dictionary<string, object?>? conversationValues = conversationKey is not null && items.TryGetValue(conversationKey, out Dictionary<string, object?>? convValue) ? convValue : null;
        Dictionary<string, object?>? userValues = userKey is not null && items.TryGetValue(userKey, out Dictionary<string, object?>? userValue) ? userValue : null;

        StateScope conversation = new(persisted: conversationKey is not null, conversationValues);
        StateScope user = new(persisted: userKey is not null, userValues);

        return new TurnState(conversation, user, conversationKey, userKey);
    }

    /// <summary>
    /// Saves changed persisted scopes: a scope emptied this turn is deleted; an otherwise-changed scope
    /// is written. Unchanged or keyless scopes are skipped. Call only when the turn succeeded.
    /// </summary>
    /// <param name="state">The turn state to persist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    internal async Task SaveAsync(TurnState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        Dictionary<string, Dictionary<string, object?>> changes = [];
        List<string> deletes = [];
        CollectChange(state.ConversationKey, state.Conversation, changes, deletes);
        CollectChange(state.UserKey, state.User, changes, deletes);

        if (changes.Count > 0)
        {
            await WriteAsync(changes, cancellationToken).ConfigureAwait(false);
        }
        if (deletes.Count > 0)
        {
            await DeleteAsync(deletes, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Routes a changed persisted scope to the right operation: a scope emptied this turn is deleted;
    /// an otherwise-changed scope is written. Unchanged or keyless scopes are skipped.
    /// </summary>
    private static void CollectChange(string? key, StateScope scope, Dictionary<string, Dictionary<string, object?>> changes, List<string> deletes)
    {
        if (key is null || !scope.IsChanged())
        {
            return;
        }

        if (scope.IsEmpty)
        {
            deletes.Add(key);
        }
        else
        {
            changes[key] = scope.Snapshot();
        }
    }

    /// <summary>
    /// Reads the documents for the given keys. <see cref="IDistributedCache"/> has no batch read, so the
    /// per-key gets are issued concurrently; a turn reads at most two keys. Missing keys are omitted.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, Dictionary<string, object?>>> ReadAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        KeyValuePair<string, byte[]?>[] reads = await Task.WhenAll(
            keys.Select(async key => new KeyValuePair<string, byte[]?>(
                key, await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false)))).ConfigureAwait(false);

        Dictionary<string, Dictionary<string, object?>> result = [];
        foreach ((string key, byte[]? bytes) in reads)
        {
            if (bytes is not null)
            {
                result[key] = StateSerializer.Deserialize(bytes);
            }
        }

        return result;
    }

    /// <summary>Writes the given documents as bare camelCase JSON, applying the configured entry options.</summary>
    private Task WriteAsync(IReadOnlyDictionary<string, Dictionary<string, object?>> changes, CancellationToken cancellationToken)
        => Task.WhenAll(changes.Select(change =>
            _cache.SetAsync(change.Key, StateSerializer.Serialize(change.Value), _entryOptions, cancellationToken)));

    /// <summary>Removes the documents for the given keys.</summary>
    private Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
        => Task.WhenAll(keys.Select(key => _cache.RemoveAsync(key, cancellationToken)));
}
