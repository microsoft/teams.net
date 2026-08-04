// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Conversation ID helpers for threaded messaging.
/// </summary>
public static class ConversationExtensions
{
    /// <summary>
    /// The thread root portion of the conversation ID, with any <c>;messageid=</c> suffix stripped.
    /// </summary>
    public static string ThreadId(this Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        string[] parts = conversation.Id.Split(';');
        return parts.Length > 1 ? parts[0] : conversation.Id;
    }

    /// <summary>
    /// Construct a threaded conversation ID by appending <c>;messageid={messageId}</c>
    /// to the conversation ID. This is the format APX uses to route messages
    /// to a specific thread in a channel.
    /// </summary>
    /// <param name="conversationId">the conversation to thread into (e.g. <c>19:abc@thread.skype</c>)</param>
    /// <param name="messageId">the thread root message ID (must be a non-zero numeric string)</param>
    /// <returns>the threaded conversation ID (e.g. <c>19:abc@thread.skype;messageid=123</c>)</returns>
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
