// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.MessageExtension;

/// <summary>
/// Message extension command context values.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<MessageExtensionCommandContext>))]
public class MessageExtensionCommandContext(string value) : StringEnum(value)
{
    /// <summary>
    /// Command invoked from a message (message action).
    /// </summary>
    public static readonly MessageExtensionCommandContext Message = new("message");

    /// <summary>
    /// Command invoked from the compose box.
    /// </summary>
    public static readonly MessageExtensionCommandContext Compose = new("compose");

    /// <summary>
    /// Command invoked from the command box.
    /// </summary>
    public static readonly MessageExtensionCommandContext CommandBox = new("commandbox");
}

/// <summary>
/// Message extension command context values.
/// </summary>
public static class MessageExtensionCommandContexts
{
    /// <summary>
    /// Command invoked from a message (message action).
    /// </summary>
    public static MessageExtensionCommandContext Message => MessageExtensionCommandContext.Message;

    /// <summary>
    /// Command invoked from the compose box.
    /// </summary>
    public static MessageExtensionCommandContext Compose => MessageExtensionCommandContext.Compose;

    /// <summary>
    /// Command invoked from the command box.
    /// </summary>
    public static MessageExtensionCommandContext CommandBox => MessageExtensionCommandContext.CommandBox;
}

/// <summary>
/// Bot message preview action values.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<BotMessagePreviewActionType>))]
public class BotMessagePreviewActionType(string value) : StringEnum(value)
{
    /// <summary>
    /// User clicked edit on the preview.
    /// </summary>
    public static readonly BotMessagePreviewActionType Edit = new("edit");

    /// <summary>
    /// User clicked send on the preview.
    /// </summary>
    public static readonly BotMessagePreviewActionType Send = new("send");
}

/// <summary>
/// Bot message preview action values.
/// </summary>
public static class BotMessagePreviewActionTypes
{
    /// <summary>
    /// User clicked edit on the preview.
    /// </summary>
    public static BotMessagePreviewActionType Edit => BotMessagePreviewActionType.Edit;

    /// <summary>
    /// User clicked send on the preview.
    /// </summary>
    public static BotMessagePreviewActionType Send => BotMessagePreviewActionType.Send;
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
    /// See <see cref="MessageExtensionCommandContexts"/> for common values.
    /// </summary>
    [JsonPropertyName("commandContext")]
    public required MessageExtensionCommandContext CommandContext { get; set; }

    /// <summary>
    /// Bot message preview action taken by user.
    /// See <see cref="BotMessagePreviewActionTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("botMessagePreviewAction")]
    public BotMessagePreviewActionType? BotMessagePreviewAction { get; set; }

    /// <summary>
    /// The activity preview that was originally sent to Teams when showing the bot message preview.
    /// This is sent back by Teams when the user clicks 'edit' or 'send' on the preview.
    /// </summary>
    // TODO : this needs to be activity type or something else - format is type, attachments[]
    [JsonPropertyName("botActivityPreview")]
    public IList<TeamsActivityInput>? BotActivityPreview { get; set; }

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
    /// See <see cref="MessagePayloadImportanceTypes"/> for common values.
    /// </remarks>
    [JsonPropertyName("importance")]
    public MessageImportance? Importance { get; set; }

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
    public AttachmentLayoutType? AttachmentLayout { get; set; }

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
[JsonConverter(typeof(StringEnumJsonConverter<MessageImportance>))]
public class MessageImportance(string value) : StringEnum(value)
{
    /// <summary>
    /// Normal importance.
    /// </summary>
    public static readonly MessageImportance Normal = new("normal");

    /// <summary>
    /// High importance.
    /// </summary>
    public static readonly MessageImportance High = new("high");

    /// <summary>
    /// Urgent importance.
    /// </summary>
    public static readonly MessageImportance Urgent = new("urgent");
}

/// <summary>
/// Common message importance values.
/// </summary>
public static class MessagePayloadImportanceTypes
{
    /// <summary>
    /// Normal importance.
    /// </summary>
    public static MessageImportance Normal => MessageImportance.Normal;

    /// <summary>
    /// High importance.
    /// </summary>
    public static MessageImportance High => MessageImportance.High;

    /// <summary>
    /// Urgent importance.
    /// </summary>
    public static MessageImportance Urgent => MessageImportance.Urgent;
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
    public AttachmentContentType? ContentType { get; set; }

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
    /// Type of attachment content. See <see cref="AttachmentContentTypes"/> for common values.
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
    public ReactionType? ReactionType { get; set; }

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
    /// Gets or sets the user identity type. See <see cref="UserIdentityTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("userIdentityType")]
    public UserIdentityType? UserIdentityType { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// String constants for user identity types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<UserIdentityType>))]
public class UserIdentityType(string value) : StringEnum(value)
{
    /// <summary>
    /// Azure Active Directory user.
    /// </summary>
    public static readonly UserIdentityType AadUser = new("aadUser");

    /// <summary>
    /// On-premise Azure Active Directory user.
    /// </summary>
    public static readonly UserIdentityType OnPremiseAadUser = new("onPremiseAadUser");

    /// <summary>
    /// Anonymous guest user.
    /// </summary>
    public static readonly UserIdentityType AnonymousGuest = new("anonymousGuest");

    /// <summary>
    /// Federated user.
    /// </summary>
    public static readonly UserIdentityType FederatedUser = new("federatedUser");
}

/// <summary>
/// String constants for user identity types.
/// </summary>
public static class UserIdentityTypes
{
    /// <summary>Azure Active Directory user.</summary>
    public static UserIdentityType AadUser => UserIdentityType.AadUser;
    /// <summary>On-premise Azure Active Directory user.</summary>
    public static UserIdentityType OnPremiseAadUser => UserIdentityType.OnPremiseAadUser;
    /// <summary>Anonymous guest user.</summary>
    public static UserIdentityType AnonymousGuest => UserIdentityType.AnonymousGuest;
    /// <summary>Federated user.</summary>
    public static UserIdentityType FederatedUser => UserIdentityType.FederatedUser;
}
