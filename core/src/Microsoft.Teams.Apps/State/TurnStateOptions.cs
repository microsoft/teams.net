// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Configuration options for turn state management.
/// </summary>
public class TurnStateOptions
{
    /// <summary>
    /// Gets or sets the cache entry options applied when saving state.
    /// Defaults to no expiration.
    /// </summary>
    public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the prefix for cache keys.
    /// Defaults to <c>"ts"</c>. Change this to isolate state when multiple bots share the same cache.
    /// Keys are formatted as <c>{KeyPrefix}:conv:{conversationId}</c> and <c>{KeyPrefix}:user:{conversationId}:{userId}</c>.
    /// </summary>
    public string KeyPrefix { get; set; } = "ts";
}
