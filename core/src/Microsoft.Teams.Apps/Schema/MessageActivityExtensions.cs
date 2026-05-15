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
    /// Adds a quoted message reference and appends a placeholder to the message text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="messageId">The ID of the message being quoted.</param>
    /// <param name="text">Optional text to append after the quote placeholder.</param>
    /// <returns>The message activity for chaining.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public static MessageActivity AddQuote(this MessageActivity message, string messageId, string? text = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        QuotedReplyEntity entity = new() { QuotedReply = new QuotedReplyData { MessageId = messageId } };
        message.Entities ??= [];
        message.Entities.Add(entity);

        message.Text = (message.Text ?? "") + QuotedReplyEntity.QuotedPlaceholder(messageId);
        if (text != null)
        {
            message.Text += $" {text}";
        }

        return message;
    }

    /// <summary>
    /// Prepends a quoted message placeholder before existing text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="messageId">The ID of the message being quoted.</param>
    /// <returns>The message activity for chaining.</returns>
    [Experimental("ExperimentalTeamsQuotedReplies")]
    public static MessageActivity PrependQuote(this MessageActivity message, string messageId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        message.Entities ??= [];
        message.Entities.Insert(0, new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });
        var placeholder = QuotedReplyEntity.QuotedPlaceholder(messageId);
        var text = message.Text?.Trim() ?? "";
        message.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";

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

        TeamsActivityExtensions.AddMention(message, account, text, addText);
        return message;
    }

    /// <summary>
    /// Marks the message as a final streaming message by adding a <see cref="StreamInfoEntity"/>
    /// with <see cref="StreamType.Final"/>.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddStreamFinal(this MessageActivity message)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ChannelData ??= new TeamsChannelData();
        if (!message.ChannelData.Properties.TryGetValue("streamId", out object? streamId) || streamId is null)
        {
            message.ChannelData.Properties["streamId"] = message.Id;
        }
        message.ChannelData.Properties["streamType"] = StreamType.Final;

        message.Entities ??= [];
        message.Entities.Add(new StreamInfoEntity
        {
            StreamId = message.Id,
            StreamType = StreamType.Final
        });
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

}
