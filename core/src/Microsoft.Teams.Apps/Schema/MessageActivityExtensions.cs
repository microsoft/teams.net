// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Fluent extension methods for <see cref="MessageActivity"/> that delegate to <see cref="TeamsActivityBuilder"/> internally.
/// These methods provide backward compatibility with the old library's <c>message.WithText(...).WithSuggestedActions(...)</c> pattern.
/// </summary>
public static class MessageActivityExtensions
{

    /// <summary>
    /// Sets the activity id.
    /// </summary>
    public static MessageActivity WithId(this MessageActivity message, string value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Id = value;
        return message;
    }

    /// <summary>
    /// Sets the channel id.
    /// </summary>
    public static MessageActivity WithChannelId(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.ChannelId = value;
        return message;
    }

    /// <summary>
    /// Sets the sender account.
    /// </summary>
    public static MessageActivity WithFrom(this MessageActivity message, ConversationAccount? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.From = value is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(value);
        return message;
    }

    /// <summary>
    /// Sets the recipient account on the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="account">The recipient account.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity WithRecipient(this MessageActivity message, ConversationAccount account)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Recipient = account is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(account);
        return message;
    }

    /// <summary>
    /// Sets the recipient account and targeted flag on the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="account">The recipient account.</param>
    /// <param name="isTargeted">Whether the recipient is targeted.</param>
    /// <returns>The message activity for chaining.</returns>
    [Experimental("ExperimentalTeamsTargeted")]
    public static MessageActivity WithRecipient(this MessageActivity message, ConversationAccount account, bool isTargeted = false)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (account is not null)
        {
            account.IsTargeted = isTargeted ? true : null;
            message.Recipient = account is TeamsConversationAccount teamsAccount
                ? teamsAccount
                : TeamsConversationAccount.FromConversationAccount(account);
        }
        return message;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    public static MessageActivity WithConversation(this MessageActivity message, Conversation? value)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Conversation = value is TeamsConversation teamsConversation
            ? teamsConversation
            : TeamsConversation.FromConversation(value);
        return message;
    }

    /// <summary>
    /// Sets the service url.
    /// </summary>
    public static MessageActivity WithServiceUrl(this MessageActivity message, Uri? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.ServiceUrl = value;
        return message;
    }

    /// <summary>
    /// Sets the locale.
    /// </summary>
    public static MessageActivity WithLocale(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Locale = value;
        return message;
    }

    /// <summary>
    /// Sets the UTC timestamp value.
    /// </summary>
    public static MessageActivity WithTimestamp(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Timestamp = value;
        return message;
    }

    /// <summary>
    /// Sets the local timestamp value.
    /// </summary>
    public static MessageActivity WithLocalTimestamp(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.LocalTimestamp = value;
        return message;
    }

    /// <summary>
    /// Sets a channel data key/value property.
    /// </summary>
    public static MessageActivity WithData(this MessageActivity message, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.Properties[key] = value;
        return message;
    }

    /// <summary>
    /// Sets the app id inside channel data.
    /// </summary>
    public static MessageActivity WithAppId(this MessageActivity message, string value)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.Properties["app"] = new Dictionary<string, object?> { ["id"] = value };
        return message;
    }

    /// <summary>
    /// Sets the text content of the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="text">The text to set.</param>
    /// <param name="textFormat">The text format. Default is "plain".</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity WithText(this MessageActivity message, string text, string textFormat = TextFormats.Plain)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Text = text;
        message.TextFormat = textFormat;
        return message;
    }

    /// <summary>
    /// Appends text to the current message text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddText(this MessageActivity message, string text)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Text = $"{message.Text}{text}";
        return message;
    }

    /// <summary>
    /// Sets the text format for the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="textFormat">The text format. See <see cref="TextFormats"/> for common values.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity WithTextFormat(this MessageActivity message, string textFormat)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.TextFormat = textFormat;
        return message;
    }

    /// <summary>
    /// Adds one or more attachments to the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="attachments">The attachments to add.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddAttachment(this MessageActivity message, params TeamsAttachment[] attachments)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(attachments);
        message.Attachments ??= [];
        foreach (TeamsAttachment attachment in attachments)
        {
            message.Attachments.Add(attachment);
        }
        return message;
    }

    /// <summary>
    /// Sets the attachment layout for the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="attachmentLayout">The attachment layout (e.g., "list", "carousel").</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity WithAttachmentLayout(this MessageActivity message, string attachmentLayout)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.AttachmentLayout = attachmentLayout;
        return message;
    }

    /// <summary>
    /// Sets the suggested actions for the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="suggestedActions">The suggested actions to set.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity WithSuggestedActions(this MessageActivity message, SuggestedActions suggestedActions)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.SuggestedActions = suggestedActions;
        return message;
    }

    /// <summary>
    /// Adds one or more entities to the message.
    /// </summary>
    /// <param name="message">The target message.</param>
    /// <param name="entities">Entities to add.</param>
    /// <returns>The message for chaining.</returns>
    public static MessageActivity AddEntity(this MessageActivity message, params Entity[] entities)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(entities);

        message.Entities ??= [];
        foreach (Entity entity in entities)
        {
            message.Entities.Add(entity);
        }

        return message;
    }

    /// <summary>
    /// Replaces an existing entity with a new entity.
    /// </summary>
    /// <param name="message">The target message.</param>
    /// <param name="oldEntity">The entity to replace.</param>
    /// <param name="newEntity">The replacement entity.</param>
    /// <returns>The message for chaining.</returns>
    public static MessageActivity UpdateEntity(this MessageActivity message, Entity oldEntity, Entity newEntity)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(oldEntity);
        ArgumentNullException.ThrowIfNull(newEntity);

        if (message.Entities != null)
        {
            message.Entities.Remove(oldEntity);
        }
        else
        {
            message.Entities = [];
        }

        message.Entities.Add(newEntity);
        return message;
    }

    /// <summary>
    /// Adds a quoted message reference and appends a placeholder to the message text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="messageId">The ID of the message being quoted.</param>
    /// <param name="text">Optional text to append after the quote placeholder.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddQuote(this MessageActivity message, string messageId, string? text = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        QuotedReplyEntityExtensions.AddToActivity(message, messageId, text);

        return message;
    }

    /// <summary>
    /// Prepends a quoted message placeholder before existing text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="messageId">The ID of the message being quoted.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity PrependQuote(this MessageActivity message, string messageId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        message.Entities ??= [];
        message.Entities.Insert(0, new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });
        string placeholder = QuotedReplyEntityExtensions.QuotedPlaceholder(messageId);
        string text = message.Text?.Trim() ?? "";
        message.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";

        return message;
    }


    /// <summary>
    /// Adds targeted message info entity for prompt preview and strips quote placeholders.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public static MessageActivity AddTargetedMessageInfo(this MessageActivity message, string messageId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        TargetedMessageInfoEntityExtensions.AddToActivity(message, messageId);

        return message;
    }

    /// <summary>
    /// Adds a mention (@mention) entity and optionally prepends mention text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="account">The account being mentioned.</param>
    /// <param name="text">Optional mention text. If null, uses account name.</param>
    /// <param name="addText">Whether mention text should be prepended to message text.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddMention(this MessageActivity message, ConversationAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(account);

        MentionEntityExtensions.AddToActivity(message, account, text, addText);

        return message;
    }

    /// <summary>
    /// Marks the message as a final streaming message by adding a <see cref="StreamInfoEntity"/>
    /// with <see cref="StreamTypes.Final"/>.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddStreamFinal(this MessageActivity message)
    {
        ArgumentNullException.ThrowIfNull(message);

        StreamInfoEntityExtensions.AddToActivity(message, StreamTypes.Final);
        return message;
    }

    /// <summary>
    /// Gets the mention entity for a specific account id.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="accountId">The account id to match.</param>
    /// <returns>The matching mention entity, or null if not found.</returns>
    public static MentionEntity? GetAccountMention(this MessageActivity message, string accountId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return (MentionEntity?)(message.Entities ?? []).FirstOrDefault(e => e is MentionEntity mention && mention.Mentioned?.Id == accountId);
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public static OMessageEntity AddAIGenerated(this MessageActivity message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return OMessageEntityExtensions.AddAIGeneratedContent(message);
    }

    /// <summary>
    /// Adds a content sensitivity label to the message.
    /// </summary>
    public static MessageActivity AddSensitivityLabel(this MessageActivity message, string name, string? description = null, DefinedTerm? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        SensitiveUsageEntityExtensions.AddToActivity(message, name, description, pattern);
        return message;
    }

    /// <summary>
    /// Enables/disables feedback loop on the message.
    /// </summary>
    public static MessageActivity AddFeedback(this MessageActivity message, bool value = true)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.FeedbackLoopEnabled = value;
        return message;
    }

    /// <summary>
    /// Configures feedback loop mode on the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="mode">The feedback loop type. See <see cref="FeedbackTypes"/> for known values.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddFeedback(this MessageActivity message, string mode)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.FeedbackLoop = new FeedbackLoop(mode);
        message.ChannelData.FeedbackLoopEnabled = null;
        return message;
    }

    /// <summary>
    /// Adds a citation claim to the message.
    /// </summary>
    public static CitationEntity AddCitation(this MessageActivity message, int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(appearance);

        message.Entities ??= [];
        return CitationEntityExtensions.AddToActivity(message, position, appearance);
    }

}
