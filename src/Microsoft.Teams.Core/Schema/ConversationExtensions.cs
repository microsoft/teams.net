// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Conversation ID helpers for threaded messaging.
/// </summary>
public static class ConversationExtensions
{
    /// <summary>
    /// The base conversation ID, with any legacy <c>;messageid=</c> suffix stripped.
    /// </summary>
    public static string ThreadId(this Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        string[] parts = conversation.Id.Split(';');
        return parts.Length > 1 ? parts[0] : conversation.Id;
    }

    /// <summary>
    /// Construct a threaded conversation ID by appending <c>;messageid={messageId}</c>
    /// to the conversation ID. This is the format the Teams service uses to route messages
    /// to a specific thread in a channel.
    /// </summary>
    /// <param name="conversationId">the conversation to thread into (e.g. <c>19:abc@thread.skype</c>)</param>
    /// <param name="messageId">the thread root message ID (must be a non-zero numeric string)</param>
    /// <returns>the threaded conversation ID (e.g. <c>19:abc@thread.skype;messageid=123</c>)</returns>
    /// <remarks>
    /// Thread placement is endpoint-based. Pass the base conversation ID and the thread root ID separately to <see cref="ConversationClient.ReplyToActivityAsync"/>, or to <c>TeamsBotApplication.ReplyAsync</c> from an app, rather than folding them into one ID here.
    /// </remarks>
    [Obsolete("Thread placement is endpoint-based. Do not fold the thread root into the conversation ID; pass it separately to TeamsBotApplication.ReplyAsync or ConversationClient.ReplyToActivityAsync.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string ToThreadedConversationId(string conversationId, string messageId)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("conversationId must be a non-empty string", nameof(conversationId));
        }

        if (string.IsNullOrEmpty(messageId) || !ulong.TryParse(messageId, out ulong parsed) || parsed == 0)
        {
            throw new ArgumentException($"Invalid messageId \"{messageId}\": must be a non-zero numeric value", nameof(messageId));
        }

        string baseId = conversationId.Split(';')[0];
        return $"{baseId};messageid={messageId}";
    }
}
