// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps;

/// <summary>
/// High-performance logging methods generated via the <see cref="LoggerMessageAttribute"/> source generator.
/// </summary>
internal static partial class Log
{
    // ── State ────────────────────────────────────────────────────────────

    [LoggerMessage(EventId = 70, Level = LogLevel.Warning, Message = "Turn state is using the in-memory cache. State will be lost on restart. Register a persistent IDistributedCache (e.g. AddStackExchangeRedisCache) for production use.")]
    public static partial void StateUsingInMemoryCache(this ILogger logger);
}
