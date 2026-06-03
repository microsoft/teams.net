// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Configuration options for turn state management.
/// </summary>
public class TurnStateOptions
{
    /// <summary>
    /// Gets or sets the cache entry options applied when saving state.
    /// Defaults to a 1-hour sliding expiration.
    /// </summary>
    public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new()
    {
        SlidingExpiration = TimeSpan.FromHours(1)
    };
}
