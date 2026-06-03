// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Middleware that loads and saves per-turn state from a distributed cache.
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

        string? sessionKey = GetSessionKey(activity);

        if (sessionKey is null)
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
            return;
        }

        byte[]? bytes = await _cache.GetAsync(sessionKey, cancellationToken).ConfigureAwait(false);
        TurnState state = TurnState.FromJsonBytes(bytes);
        botApplication.TurnState = state;

        try
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (state.IsDirty)
            {
                await _cache.SetAsync(sessionKey, state.ToJsonBytes(), _options.CacheEntryOptions, cancellationToken).ConfigureAwait(false);
            }

            botApplication.TurnState = null;
        }
    }

    private static string? GetSessionKey(CoreActivity activity)
    {
        string? conversationId = activity.Conversation?.Id;
        string? fromId = activity.From?.Id;

        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(fromId))
        {
            return null;
        }

        return $"ts:{conversationId}:{fromId}";
    }
}
