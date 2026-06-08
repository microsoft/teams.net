// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Loads and saves per-turn state from a distributed cache.
/// Manages two state scopes: conversation-scoped and user-scoped.
/// </summary>
internal sealed class TurnStateLoader
{
    private readonly IDistributedCache _cache;
    private readonly TurnStateOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TurnStateLoader"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache used to persist turn state.</param>
    /// <param name="options">Options controlling cache entry lifetime.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public TurnStateLoader(IDistributedCache cache, IOptions<TurnStateOptions> options, ILogger<TurnStateLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        _cache = cache;
        _options = options.Value;

        if (cache is MemoryDistributedCache)
        {
            logger.StateUsingInMemoryCache();
        }
    }

    /// <summary>
    /// Loads conversation and user state from the cache.
    /// </summary>
    public async Task<TurnStateContainer> LoadAsync(string conversationId, string? userId, CancellationToken cancellationToken)
    {
        string conversationKey = $"{_options.KeyPrefix}:conv:{conversationId}";
        TurnState conversationState = await LoadStateAsync(conversationKey, cancellationToken).ConfigureAwait(false);

        TurnState? userState = null;
        if (!string.IsNullOrEmpty(userId))
        {
            string userKey = $"{_options.KeyPrefix}:user:{conversationId}:{userId}";
            userState = await LoadStateAsync(userKey, cancellationToken).ConfigureAwait(false);
        }

        return new TurnStateContainer(conversationState, userState);
    }

    /// <summary>
    /// Saves dirty state back to the cache.
    /// </summary>
    public async Task SaveAsync(TurnStateContainer container, string conversationId, string? userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (container.ConversationState is TurnState conversationState && conversationState.IsDirty)
        {
            string conversationKey = $"{_options.KeyPrefix}:conv:{conversationId}";
            await SaveStateAsync(conversationKey, conversationState, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(userId) && container.UserState is TurnState userState && userState.IsDirty)
        {
            string userKey = $"{_options.KeyPrefix}:user:{conversationId}:{userId}";
            await SaveStateAsync(userKey, userState, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes conversation and/or user state from the cache.
    /// </summary>
    public async Task DeleteAsync(string conversationId, string? userId, CancellationToken cancellationToken)
    {
        string conversationKey = $"{_options.KeyPrefix}:conv:{conversationId}";
        await _cache.RemoveAsync(conversationKey, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(userId))
        {
            string userKey = $"{_options.KeyPrefix}:user:{conversationId}:{userId}";
            await _cache.RemoveAsync(userKey, cancellationToken).ConfigureAwait(false);
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
