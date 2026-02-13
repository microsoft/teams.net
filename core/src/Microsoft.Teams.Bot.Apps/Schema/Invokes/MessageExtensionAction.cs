// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Message extension command context values.
/// </summary>
public static class MessageExtensionCommandContext
{
    /// <summary>
    /// Command invoked from a message (message action).
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Command invoked from the compose box.
    /// </summary>
    public const string Compose = "compose";

    /// <summary>
    /// Command invoked from the command box.
    /// </summary>
    public const string CommandBox = "commandbox";
}

/// <summary>
/// Bot message preview action values.
/// </summary>
public static class BotMessagePreviewAction
{
    /// <summary>
    /// User clicked edit on the preview.
    /// </summary>
    public const string Edit = "edit";

    /// <summary>
    /// User clicked send on the preview.
    /// </summary>
    public const string Send = "send";
}

/// <summary>
/// Context information for message extension actions.
/// </summary>
public class MessageExtensionContext
{
    /// <summary>
    /// The theme of the Teams client. Common values: "default", "dark", "contrast".
    /// </summary>
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
}

/// <summary>
/// Message extension action payload for submit action and fetch task activities.
/// </summary>
public class MessageExtensionAction
{
    /// <summary>
    /// Id of the command assigned by the bot.
    /// </summary>
    [JsonPropertyName("commandId")]
    public required string CommandId { get; set; }

    /// <summary>
    /// The context from which the command originates.
    /// See <see cref="MessageExtensionCommandContext"/> for common values.
    /// </summary>
    [JsonPropertyName("commandContext")]
    public required string CommandContext { get; set; }

    /// <summary>
    /// Bot message preview action taken by user.
    /// See <see cref="BotMessagePreviewAction"/> for common values.
    /// </summary>
    [JsonPropertyName("botMessagePreviewAction")]
    public string? BotMessagePreviewAction { get; set; }

    /// <summary>
    /// The activity preview that was originally sent to Teams when showing the bot message preview.
    /// This is sent back by Teams when the user clicks 'edit' or 'send' on the preview.
    /// </summary>
    // TODO : this needs to be activity type or something else - format is type, attachments[]
    [JsonPropertyName("botActivityPreview")]
    public IList<TeamsActivity>? BotActivityPreview { get; set; }

    /// <summary>
    /// Data included with the submit action.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Message content sent as part of the command request when the command is invoked from a message.
    /// </summary>
    [JsonPropertyName("messagePayload")]
    public MessagePayload? MessagePayload { get; set; }

    /// <summary>
    /// Context information for the action.
    /// </summary>
    [JsonPropertyName("context")]
    public MessageExtensionContext? Context { get; set; }
}


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
    public required string Id { get; set; }

    /// <summary>
    /// Timestamp of when the message was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// Indicates whether a message has been soft deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool? Deleted { get; set; }

    /// <summary>
    /// Subject line of the message.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The importance of the message.
    /// </summary>
    /// <remarks>
    /// See <see cref="MessagePayloadImportance"/> for common values.
    /// </remarks>
    [JsonPropertyName("importance")]
    public string? Importance { get; set; }

    /// <summary>
    /// Locale of the message set by the client.
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Link back to the message.
    /// </summary>
    [JsonPropertyName("linkToMessage")]
    public string? LinkToMessage { get; set; }

    /// <summary>
    /// Sender of the message.
    /// </summary>
    [JsonPropertyName("from")]
    public MessageFrom? From { get; set; }

    /// <summary>
    /// Plaintext/HTML representation of the content of the message.
    /// </summary>
    [JsonPropertyName("body")]
    public MessagePayloadBody? Body { get; set; }

    /// <summary>
    /// How the attachment(s) are displayed in the message.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public string? AttachmentLayout { get; set; }

    /// <summary>
    /// Attachments in the message - card, image, file, etc.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IList<MessagePayloadAttachment>? Attachments { get; set; }

    /// <summary>
    /// List of entities mentioned in the message.
    /// </summary>
    [JsonPropertyName("mentions")]
    public IList<MentionEntity>? Mentions { get; set; }

    /// <summary>
    /// Reactions for the message.
    /// </summary>
    [JsonPropertyName("reactions")]
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
/// String constants for message importance levels.
/// </summary>
public static class MessagePayloadImportance
{
    /// <summary>
    /// Normal importance.
    /// </summary>
    public const string Normal = "normal";

    /// <summary>
    /// High importance.
    /// </summary>
    public const string High = "high";

    /// <summary>
    /// Urgent importance.
    /// </summary>
    public const string Urgent = "urgent";
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