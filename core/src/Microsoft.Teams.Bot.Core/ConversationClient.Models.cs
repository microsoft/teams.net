// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core;

/// <summary>
/// Response from sending an activity.
/// </summary>
public class SendActivityResponse
{
    /// <summary>
    /// Id of the activity
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Response from updating an activity.
/// </summary>
public class UpdateActivityResponse
{
    /// <summary>
    /// Id of the activity
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Response from deleting an activity.
/// </summary>
public class DeleteActivityResponse
{
    /// <summary>
    /// Id of the activity
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Response from getting conversations.
/// </summary>
public class GetConversationsResponse
{
    /// <summary>
    /// Gets or sets the continuation token that can be used to get paged results.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Gets or sets the list of conversations.
    /// </summary>
    [JsonPropertyName("conversations")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<ConversationMembers>? Conversations { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Represents a conversation and its members.
/// </summary>
public class ConversationMembers
{
    /// <summary>
    /// Gets or sets the conversation ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the list of members in this conversation.
    /// </summary>
    [JsonPropertyName("members")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<ConversationAccount>? Members { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Parameters for creating a new conversation.
/// </summary>
public class ConversationParameters
{
    /// <summary>
    /// Gets or sets a value indicating whether the conversation is a group conversation.
    /// </summary>
    [JsonPropertyName("isGroup")]
    public bool? IsGroup { get; set; }

    /// <summary>
    /// Gets or sets the bot's account for this conversation.
    /// </summary>
    [JsonPropertyName("bot")]
    public ConversationAccount? Bot { get; set; }

    /// <summary>
    /// Gets or sets the list of members to add to the conversation.
    /// </summary>
    [JsonPropertyName("members")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<ConversationAccount>? Members { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets the topic name for the conversation (if supported by the channel).
    /// </summary>
    [JsonPropertyName("topicName")]
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets the initial activity to send when creating the conversation.
    /// </summary>
    [JsonPropertyName("activity")]
    public CoreActivity? Activity { get; set; }

    /// <summary>
    /// Gets or sets channel-specific payload for creating the conversation.
    /// </summary>
    [JsonPropertyName("channelData")]
    public object? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID where the conversation should be created.
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }
}

/// <summary>
/// Response from creating a conversation.
/// </summary>
public class CreateConversationResponse
{
    /// <summary>
    /// Gets or sets the ID of the activity (if sent).
    /// </summary>
    [JsonPropertyName("activityId")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the service endpoint where operations concerning the conversation may be performed.
    /// </summary>
    [JsonPropertyName("serviceUrl")]
    public Uri? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the conversation resource.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Result from getting paged members of a conversation.
/// </summary>
public class PagedMembersResult
{
    /// <summary>
    /// Gets or sets the continuation token that can be used to get paged results.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Gets or sets the list of members in this page.
    /// </summary>
    [JsonPropertyName("members")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<ConversationAccount>? Members { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// A collection of activities that represents a conversation transcript.
/// </summary>
public class Transcript
{
    /// <summary>
    /// Gets or sets the collection of activities that conforms to the Transcript schema.
    /// </summary>
    [JsonPropertyName("activities")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<CoreActivity>? Activities { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}

/// <summary>
/// Response from sending conversation history.
/// </summary>
public class SendConversationHistoryResponse
{
    /// <summary>
    /// Gets or sets the ID of the resource.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Represents attachment data for uploading.
/// </summary>
public class AttachmentData
{
    /// <summary>
    /// Gets or sets the Content-Type of the attachment.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the attachment.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the attachment content as a byte array.
    /// </summary>
    [JsonPropertyName("originalBase64")]
#pragma warning disable CA1819 // Properties should not return arrays
    public byte[]? OriginalBase64 { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>
    /// Gets or sets the attachment thumbnail as a byte array.
    /// </summary>
    [JsonPropertyName("thumbnailBase64")]
#pragma warning disable CA1819 // Properties should not return arrays
    public byte[]? ThumbnailBase64 { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
}

/// <summary>
/// Response from uploading an attachment.
/// </summary>
public class UploadAttachmentResponse
{
    /// <summary>
    /// Gets or sets the ID of the uploaded attachment.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
