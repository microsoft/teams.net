// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Middleware that loads and saves per-turn state from a distributed cache.
/// Manages two state scopes: conversation-scoped and user-scoped.
/// </summary>
public class TurnStateMiddleware : ITurnMiddleware
{
    private readonly IDistributedCache _cache;
    private readonly TurnStateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TurnStateMiddleware"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache used to persist turn state.</param>
    /// <param name="options">Options controlling cache entry lifetime.</param>
    public TurnStateMiddleware(IDistributedCache cache, IOptions<TurnStateOptions> options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        _cache = cache;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(botApplication);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(nextTurn);

        string? conversationId = activity.Conversation?.Id;

        if (string.IsNullOrEmpty(conversationId))
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
            return;
        }

        string conversationKey = $"ts:conv:{conversationId}";
        string? userId = activity.From?.Id;
        string? userKey = string.IsNullOrEmpty(userId) ? null : $"ts:user:{conversationId}:{userId}";

        TurnState conversationState = await LoadStateAsync(conversationKey, cancellationToken).ConfigureAwait(false);

        TurnState? userState = null;
        if (userKey is not null)
        {
            userState = await LoadStateAsync(userKey, cancellationToken).ConfigureAwait(false);
        }

        botApplication.State = new TurnStateContainer(conversationState, userState);

        try
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (conversationState.IsDirty)
            {
                await SaveStateAsync(conversationKey, conversationState, cancellationToken).ConfigureAwait(false);
            }

            if (userState is not null && userState.IsDirty)
            {
                await SaveStateAsync(userKey!, userState, cancellationToken).ConfigureAwait(false);
            }

            botApplication.State = null;
        }
    }

    private async Task<TurnState> LoadStateAsync(string key, CancellationToken cancellationToken)
    {
        byte[]? bytes = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return TurnState.FromJsonBytes(bytes);
    }

    private async Task SaveStateAsync(string key, TurnState state, CancellationToken cancellationToken)
    {
        await _cache.SetAsync(key, state.ToJsonBytes(), _options.CacheEntryOptions, cancellationToken).ConfigureAwait(false);
    }
}
