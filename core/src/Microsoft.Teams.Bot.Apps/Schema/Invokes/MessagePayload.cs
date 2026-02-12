// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Represents the individual message within a chat or channel where a message
/// action is taken.
/// </summary>
public class MessagePayload
{
    /// <summary>
    /// Unique id of the message.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// Timestamp of when the message was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    [JsonPropertyOrder(3)]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// Indicates whether a message has been soft deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonPropertyOrder(5)]
    public bool? Deleted { get; set; }

    /// <summary>
    /// Subject line of the message.
    /// </summary>
    [JsonPropertyName("subject")]
    [JsonPropertyOrder(6)]
    public string? Subject { get; set; }

    /// <summary>
    /// The importance of the message.
    /// </summary>
    [JsonPropertyName("importance")]
    [JsonPropertyOrder(8)]
    public MessagePayloadImportance? Importance { get; set; }

    /// <summary>
    /// Locale of the message set by the client.
    /// </summary>
    [JsonPropertyName("locale")]
    [JsonPropertyOrder(9)]
    public string? Locale { get; set; }

    /// <summary>
    /// Link back to the message.
    /// </summary>
    [JsonPropertyName("linkToMessage")]
    [JsonPropertyOrder(10)]
    public string? LinkToMessage { get; set; }

    /// <summary>
    /// Sender of the message.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonPropertyOrder(11)]
    public MessageFrom? From { get; set; }

    /// <summary>
    /// Plaintext/HTML representation of the content of the message.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(12)]
    public MessagePayloadBody? Body { get; set; }

    /// <summary>
    /// How the attachment(s) are displayed in the message.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    [JsonPropertyOrder(13)]
    public string? AttachmentLayout { get; set; }

    /// <summary>
    /// Attachments in the message - card, image, file, etc.
    /// </summary>
    [JsonPropertyName("attachments")]
    [JsonPropertyOrder(14)]
    public IList<MessagePayloadAttachment>? Attachments { get; set; }

    /// <summary>
    /// List of entities mentioned in the message.
    /// </summary>
    [JsonPropertyName("mentions")]
    [JsonPropertyOrder(15)]
    public IList<MentionEntity>? Mentions { get; set; }

    /// <summary>
    /// Reactions for the message.
    /// </summary>
    [JsonPropertyName("reactions")]
    [JsonPropertyOrder(16)]
    public IList<MessageReaction>? Reactions { get; set; }
}

/// <summary>
/// Sender of the message.
/// </summary>
public class MessageFrom
{
    /// <summary>
    /// User information of the sender.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
}

/// <summary>
/// Message importance levels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MessagePayloadImportance>))]
public enum MessagePayloadImportance
{
    /// <summary>
    /// Normal importance.
    /// </summary>
    [JsonPropertyName("normal")]
    Normal,

    /// <summary>
    /// High importance.
    /// </summary>
    [JsonPropertyName("high")]
    High,

    /// <summary>
    /// Urgent importance.
    /// </summary>
    [JsonPropertyName("urgent")]
    Urgent
}

/// <summary>
/// Message body content.
/// </summary>
public class MessagePayloadBody
{
    /// <summary>
    /// Type of content. Common values: "text", "html".
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>
/// Attachment in a message payload.
/// </summary>
public class MessagePayloadAttachment
{
    /// <summary>
    /// Unique identifier for the attachment.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Type of attachment content. See <see cref="AttachmentContentType"/> for common values.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// The attachment content.
    /// </summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

/// <summary>
/// Reaction to a message.
/// </summary>
public class MessagePayloadReaction
{
    /// <summary>
    /// Type of reaction
    /// See <see cref="ReactionTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("reactionType")]
    public string? ReactionType { get; set; }

    /// <summary>
    /// Timestamp when the reaction was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// User who reacted.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
}



/// <summary>
/// Represents a user who created a reaction.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the user identity type.
    /// </summary>
    [JsonPropertyName("userIdentityType")]
    public string? UserIdentityType { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// String constants for user identity types.
/// </summary>
public static class UserIdentityTypes
{
    /// <summary>
    /// Azure Active Directory user.
    /// </summary>
    public const string AadUser = "aadUser";

    /// <summary>
    /// On-premise Azure Active Directory user.
    /// </summary>
    public const string OnPremiseAadUser = "onPremiseAadUser";

    /// <summary>
    /// Anonymous guest user.
    /// </summary>
    public const string AnonymousGuest = "anonymousGuest";

    /// <summary>
    /// Federated user.
    /// </summary>
    public const string FederatedUser = "federatedUser";
}
