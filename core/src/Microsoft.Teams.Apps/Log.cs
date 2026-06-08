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

    [LoggerMessage(EventId = 71, Level = LogLevel.Debug, Message = "State loaded for conversation {ConversationId} (conv={ConvHit}, user={UserHit})")]
    public static partial void StateLoaded(this ILogger logger, string conversationId, bool convHit, bool userHit);

    [LoggerMessage(EventId = 72, Level = LogLevel.Debug, Message = "State saved for conversation {ConversationId} (conv={ConvDirty}, user={UserDirty})")]
    public static partial void StateSaved(this ILogger logger, string conversationId, bool convDirty, bool userDirty);

    [LoggerMessage(EventId = 73, Level = LogLevel.Debug, Message = "State deleted for conversation {ConversationId}")]
    public static partial void StateDeleted(this ILogger logger, string conversationId);

    [LoggerMessage(EventId = 74, Level = LogLevel.Warning, Message = "State load failed for conversation {ConversationId}")]
    public static partial void StateLoadFailed(this ILogger logger, Exception ex, string conversationId);

    [LoggerMessage(EventId = 75, Level = LogLevel.Warning, Message = "State save failed for conversation {ConversationId}")]
    public static partial void StateSaveFailed(this ILogger logger, Exception ex, string conversationId);
}
