// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Core.Diagnostics;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Loads and saves per-turn state from a distributed cache.
/// Manages two state scopes: conversation-scoped and user-scoped.
/// </summary>
public sealed class TurnStateLoader
{
    private readonly IDistributedCache _cache;
    private readonly TurnStateOptions _options;
    private readonly ILogger<TurnStateLoader> _logger;

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
        _logger = logger;

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
        using Activity? span = AppsTelemetry.Source.StartActivity(AppsTelemetry.Spans.State, ActivityKind.Internal);
        span?.SetTag(AppsTelemetry.Tags.StateOperation, AppsTelemetry.StateOperations.Load);
        long startTs = Stopwatch.GetTimestamp();

        try
        {
            string conversationKey = $"{_options.KeyPrefix}:conv:{conversationId}";
            byte[]? convBytes = await _cache.GetAsync(conversationKey, cancellationToken).ConfigureAwait(false);
            TurnState conversationState = TurnState.FromJsonBytes(convBytes);

            byte[]? userBytes = null;
            TurnState? userState = null;
            if (!string.IsNullOrEmpty(userId))
            {
                string userKey = $"{_options.KeyPrefix}:user:{conversationId}:{userId}";
                userBytes = await _cache.GetAsync(userKey, cancellationToken).ConfigureAwait(false);
                userState = TurnState.FromJsonBytes(userBytes);
            }

            long bytesRead = (convBytes?.Length ?? 0) + (userBytes?.Length ?? 0);
            span?.SetTag(AppsTelemetry.Tags.StateConversationHit, convBytes is not null);
            span?.SetTag(AppsTelemetry.Tags.StateUserHit, userBytes is not null);
            span?.SetTag(AppsTelemetry.Tags.StateBytesRead, bytesRead);
            AppsTelemetry.StateBytesRead.Record(bytesRead);
            _logger.StateLoaded(conversationId, convBytes is not null, userBytes is not null);

            return new TurnStateContainer(conversationState, userState);
        }
        catch (Exception ex)
        {
            span?.RecordException(ex);
            AppsTelemetry.StateCacheErrors.Add(1, new KeyValuePair<string, object?>(AppsTelemetry.Tags.StateOperation, AppsTelemetry.StateOperations.Load));
            _logger.StateLoadFailed(ex, conversationId);
            throw;
        }
        finally
        {
            AppsTelemetry.StateLoadDuration.Record(Stopwatch.GetElapsedTime(startTs).TotalMilliseconds);
        }
    }

    /// <summary>
    /// Saves dirty state back to the cache.
    /// </summary>
    public async Task SaveAsync(TurnStateContainer container, string conversationId, string? userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(container);

        using Activity? span = AppsTelemetry.Source.StartActivity(AppsTelemetry.Spans.State, ActivityKind.Internal);
        span?.SetTag(AppsTelemetry.Tags.StateOperation, AppsTelemetry.StateOperations.Save);
        long startTs = Stopwatch.GetTimestamp();

        try
        {
            bool convDirty = container.ConversationState.IsDirty;
            bool userDirty = !string.IsNullOrEmpty(userId) && container.UserState is not null && container.UserState.IsDirty;
            long bytesWritten = 0;

            if (convDirty)
            {
                string conversationKey = $"{_options.KeyPrefix}:conv:{conversationId}";
                byte[] bytes = container.ConversationState.ToJsonBytes();
                bytesWritten += bytes.Length;
                await _cache.SetAsync(conversationKey, bytes, _options.CacheEntryOptions, cancellationToken).ConfigureAwait(false);
            }

            if (userDirty)
            {
                string userKey = $"{_options.KeyPrefix}:user:{conversationId}:{userId}";
                byte[] bytes = container.UserState!.ToJsonBytes();
                bytesWritten += bytes.Length;
                await _cache.SetAsync(userKey, bytes, _options.CacheEntryOptions, cancellationToken).ConfigureAwait(false);
            }

            span?.SetTag(AppsTelemetry.Tags.StateConversationDirty, convDirty);
            span?.SetTag(AppsTelemetry.Tags.StateUserDirty, userDirty);
            span?.SetTag(AppsTelemetry.Tags.StateBytesWritten, bytesWritten);
            AppsTelemetry.StateBytesWritten.Record(bytesWritten);

            if (convDirty || userDirty)
            {
                _logger.StateSaved(conversationId, convDirty, userDirty);
            }
        }
        catch (Exception ex)
        {
            span?.RecordException(ex);
            AppsTelemetry.StateCacheErrors.Add(1, new KeyValuePair<string, object?>(AppsTelemetry.Tags.StateOperation, AppsTelemetry.StateOperations.Save));
            _logger.StateSaveFailed(ex, conversationId);
            throw;
        }
        finally
        {
            AppsTelemetry.StateSaveDuration.Record(Stopwatch.GetElapsedTime(startTs).TotalMilliseconds);
        }
    }

    /// <summary>
    /// Removes conversation and/or user state from the cache.
    /// </summary>
    public async Task DeleteAsync(string conversationId, string? userId, CancellationToken cancellationToken)
    {
        using Activity? span = AppsTelemetry.Source.StartActivity(AppsTelemetry.Spans.State, ActivityKind.Internal);
        span?.SetTag(AppsTelemetry.Tags.StateOperation, AppsTelemetry.StateOperations.Delete);

        try
        {
            string conversationKey = $"{_options.KeyPrefix}:conv:{conversationId}";
            await _cache.RemoveAsync(conversationKey, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(userId))
            {
                string userKey = $"{_options.KeyPrefix}:user:{conversationId}:{userId}";
                await _cache.RemoveAsync(userKey, cancellationToken).ConfigureAwait(false);
            }

            _logger.StateDeleted(conversationId);
        }
        catch (Exception ex)
        {
            span?.RecordException(ex);
            AppsTelemetry.StateCacheErrors.Add(1, new KeyValuePair<string, object?>(AppsTelemetry.Tags.StateOperation, AppsTelemetry.StateOperations.Delete));
            throw;
        }
    }
}
