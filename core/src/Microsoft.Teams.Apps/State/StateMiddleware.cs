// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Turn middleware that loads <see cref="TurnState"/> at the start of a turn and saves changed
/// scopes when the turn completes successfully.
/// </summary>
/// <remarks>
/// The loaded <see cref="TurnState"/> is published as <see cref="TurnState.Current"/> before the rest
/// of the pipeline runs and cleared afterwards, so downstream code (e.g. <c>Context.State</c>) resolves
/// the correct per-turn instance without it being threaded through every call. If the turn throws, the
/// save is skipped and no writes occur (atomic save). Persisted scopes are written only when their
/// serialized form differs from the load-time baseline.
/// </remarks>
public sealed class StateMiddleware : ITurnMiddleware
{
    private static readonly IReadOnlyDictionary<string, StoreItem> EmptyItems = new Dictionary<string, StoreItem>();

    private readonly IStorage _storage;
    private readonly ILogger _logger;

    /// <summary>Initializes a new <see cref="StateMiddleware"/> backed by the given storage.</summary>
    /// <param name="storage">The backing store for state documents.</param>
    /// <param name="logger">Optional logger.</param>
    public StateMiddleware(IStorage storage, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Adapts the Core <see cref="ITurnMiddleware"/> contract to the Teams-typed
    /// <see cref="OnTurnAsync(TeamsBotApplication, TeamsActivity, NextTurn, CancellationToken)"/>.
    /// State is only registered on a <see cref="TeamsBotApplication"/>, so the cast always succeeds.
    /// </summary>
    Task ITurnMiddleware.OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken)
        => OnTurnAsync((TeamsBotApplication)botApplication, TeamsActivity.FromActivity(activity), nextTurn, cancellationToken);

    /// <summary>Loads turn state, runs the rest of the pipeline, then saves changed scopes on success.</summary>
    /// <param name="botApplication">The Teams bot application processing the turn.</param>
    /// <param name="activity">The incoming Teams activity.</param>
    /// <param name="nextTurn">Delegate that runs the rest of the pipeline.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task OnTurnAsync(TeamsBotApplication botApplication, TeamsActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(nextTurn);

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

        IReadOnlyDictionary<string, StoreItem> items = keys.Count > 0
            ? await _storage.ReadAsync(keys, cancellationToken).ConfigureAwait(false)
            : EmptyItems;

        StoreItem? conversationItem = conversationKey is not null && items.TryGetValue(conversationKey, out StoreItem? convValue) ? convValue : null;
        StoreItem? userItem = userKey is not null && items.TryGetValue(userKey, out StoreItem? userValue) ? userValue : null;

        StateScope conversation = new(persisted: conversationKey is not null, conversationItem);
        StateScope user = new(persisted: userKey is not null, userItem);
        StateScope temp = new(persisted: false, loaded: null);

        TurnState turnState = new(conversation, user, temp);
        TurnState.SetCurrent(turnState);
        _logger.LogDebug("Loaded turn state (conversationKey={ConversationKey} userKey={UserKey}).", conversationKey, userKey);

        try
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);

            Dictionary<string, StoreItem> changes = [];
            List<string> deletes = [];
            CollectChange(conversationKey, conversation, changes, deletes);
            CollectChange(userKey, user, changes, deletes);

            if (changes.Count > 0)
            {
                await _storage.WriteAsync(changes, cancellationToken).ConfigureAwait(false);
            }
            if (deletes.Count > 0)
            {
                await _storage.DeleteAsync(deletes, cancellationToken).ConfigureAwait(false);
            }
            if (changes.Count > 0 || deletes.Count > 0)
            {
                _logger.LogDebug("Saved turn state ({Written} written, {Deleted} deleted).", changes.Count, deletes.Count);
            }
        }
        finally
        {
            turnState.Complete();
            TurnState.SetCurrent(null);
        }
    }

    /// <summary>
    /// Routes a changed persisted scope to the right operation: a scope emptied this turn is deleted
    /// from storage; an otherwise-changed scope is written. Unchanged or keyless scopes are skipped.
    /// </summary>
    private static void CollectChange(string? key, StateScope scope, Dictionary<string, StoreItem> changes, List<string> deletes)
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
            changes[key] = scope.ToStoreItem();
        }
    }
}
