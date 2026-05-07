// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Represents a conversation, including its unique identifier and associated extended properties.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Conversation"/> class.
/// </remarks>
public class Conversation(string id = "")
{
    /// <summary>
    /// Gets or sets the unique identifier for the object.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = id;

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData]
    public ExtendedPropertiesDictionary Properties { get; set; } = [];

    /// <summary>
    /// The thread root portion of the conversation ID, with any <c>;messageid=</c> suffix stripped.
    /// </summary>
    [JsonIgnore]
    public string ThreadId
    {
        get
        {
            var parts = Id.Split(';');
            return parts.Length > 1 ? parts[0] : Id;
        }
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

        if (string.IsNullOrEmpty(messageId) || !ulong.TryParse(messageId, out var parsed) || parsed == 0)
        {
            throw new ArgumentException($"Invalid messageId \"{messageId}\": must be a non-zero numeric value", nameof(messageId));
        }

        var baseId = conversationId.Split(';')[0];
        return $"{baseId};messageid={messageId}";
    }
}
