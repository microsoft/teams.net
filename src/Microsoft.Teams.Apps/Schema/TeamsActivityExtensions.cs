// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Threading helpers for Teams activities.
/// </summary>
public static class TeamsActivityExtensions
{
    private const string LegacyThreadMarker = ";messageid=";

    /// <summary>
    /// Gets the base conversation ID and thread root ID needed to send a proactive threaded reply.
    /// </summary>
    /// <remarks>
    /// The thread root is resolved from typed channel data first, then from a legacy
    /// <c>;messageid=</c> conversation ID suffix, and finally from the inbound activity ID.
    /// </remarks>
    /// <param name="activity">The inbound Teams activity.</param>
    /// <returns>The base conversation ID and thread root ID.</returns>
    public static (string ConversationId, string ThreadRootId) GetProactiveThreadReference(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.Conversation);

        string? threadRootId = GetExplicitThreadId(activity);
        if (string.IsNullOrWhiteSpace(threadRootId))
        {
            ArgumentException.ThrowIfNullOrEmpty(activity.Id);
            threadRootId = activity.Id;
        }

        return (activity.Conversation.ThreadId(), threadRootId);
    }

    /// <summary>
    /// Gets the thread root ID used by default for a reactive reply.
    /// </summary>
    /// <remarks>
    /// The thread root is resolved from typed channel data first, then from a legacy
    /// <c>;messageid=</c> conversation ID suffix. For a channel root activity, the inbound
    /// activity ID is used. Group-chat and personal root activities return <see langword="null"/>.
    /// </remarks>
    /// <param name="activity">The inbound Teams activity.</param>
    /// <returns>The default thread root ID, or <see langword="null"/> for an unthreaded activity.</returns>
    public static string? GetDefaultThreadId(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        string? threadRootId = GetExplicitThreadId(activity);
        if (!string.IsNullOrWhiteSpace(threadRootId))
        {
            return threadRootId;
        }

        bool isChannel = activity.Conversation?.ConversationType?.Equals(ConversationTypes.Channel) ?? false;
        return isChannel && !string.IsNullOrWhiteSpace(activity.Id) ? activity.Id : null;
    }

    private static string? GetExplicitThreadId(TeamsActivity activity)
    {
        if (!string.IsNullOrWhiteSpace(activity.ChannelData?.Thread?.Id))
        {
            return activity.ChannelData.Thread.Id;
        }

        string? conversationId = activity.Conversation?.Id;
        if (string.IsNullOrEmpty(conversationId))
        {
            return null;
        }

        int markerIndex = conversationId.IndexOf(LegacyThreadMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        string threadRootId = conversationId[(markerIndex + LegacyThreadMarker.Length)..];
        return string.IsNullOrWhiteSpace(threadRootId) ? null : threadRootId;
    }
}
