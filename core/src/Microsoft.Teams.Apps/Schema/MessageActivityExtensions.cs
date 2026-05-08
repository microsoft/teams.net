// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// Marks the message as a final streaming message by adding a <see cref="StreamInfoEntity"/>
    /// with <see cref="StreamType.Final"/>.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddStreamFinal(this MessageActivity message)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Entities ??= [];
        message.Entities.Add(new StreamInfoEntity { StreamType = StreamType.Final });
        return message;
    }
}
