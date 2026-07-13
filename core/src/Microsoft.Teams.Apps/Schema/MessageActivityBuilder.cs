// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Provides a fluent API for building <see cref="MessageActivity"/> instances.
/// This is the only supported way to construct a <see cref="MessageActivity"/>.
/// </summary>
public class MessageActivityBuilder : TeamsActivityBuilder<MessageActivity, MessageActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivityBuilder"/> class.
    /// </summary>
    internal MessageActivityBuilder() : base(new MessageActivity())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivityBuilder"/> class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal MessageActivityBuilder(MessageActivity activity) : base(activity)
    {
    }

    // ==================== Text ====================

    /// <summary>
    /// Sets the text content (and optional text format) of the message.
    /// </summary>
    public MessageActivityBuilder WithText(string text, string textFormat = TextFormats.Plain)
    {
        _activity.Text = text;
        _activity.TextFormat = textFormat;
        return this;
    }

    /// <summary>
    /// Appends text to the current message text.
    /// </summary>
    public MessageActivityBuilder AddText(string text)
    {
        _activity.Text = $"{_activity.Text}{text}";
        return this;
    }

    /// <summary>
    /// Sets the text format. See <see cref="TextFormats"/>.
    /// </summary>
    public MessageActivityBuilder WithTextFormat(string textFormat)
    {
        _activity.TextFormat = textFormat;
        return this;
    }

    // ==================== Attachments ====================

    /// <summary>
    /// Sets the attachments collection.
    /// </summary>
    public MessageActivityBuilder WithAttachments(IList<TeamsAttachment> attachments)
    {
        _activity.Attachments = attachments;
        return this;
    }

    /// <summary>
    /// Adds one or more attachments to the message.
    /// </summary>
    public MessageActivityBuilder AddAttachment(params TeamsAttachment[] attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        _activity.Attachments ??= [];
        foreach (TeamsAttachment attachment in attachments)
        {
            _activity.Attachments.Add(attachment);
        }
        return this;
    }

    /// <summary>
    /// Sets the attachment layout (e.g., "list", "carousel").
    /// </summary>
    public MessageActivityBuilder WithAttachmentLayout(string attachmentLayout)
    {
        _activity.AttachmentLayout = attachmentLayout;
        return this;
    }

    /// <summary>
    /// Adds an Adaptive Card attachment to the message.
    /// </summary>
    public MessageActivityBuilder AddAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
        => AddAttachment(BuildAdaptiveCardAttachment(adaptiveCard, configure));

    /// <summary>
    /// Sets the attachments collection to a single Adaptive Card attachment.
    /// </summary>
    public MessageActivityBuilder WithAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
        => WithAttachments([BuildAdaptiveCardAttachment(adaptiveCard, configure)]);

    // ==================== Message-specific quote/stream helpers ====================

    /// <summary>
    /// Prepends a quoted message placeholder before existing text.
    /// </summary>
    public MessageActivityBuilder PrependQuote(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        _activity.Entities ??= [];
        _activity.Entities.Insert(0, new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });
        string placeholder = QuotedReplyEntityExtensions.QuotedPlaceholder(messageId);
        string existing = _activity.Text?.Trim() ?? "";
        _activity.Text = string.IsNullOrEmpty(existing) ? placeholder : $"{placeholder} {existing}";
        return this;
    }

    /// <summary>
    /// Marks the message as a final streaming message by adding a <see cref="StreamInfoEntity"/>
    /// with <see cref="StreamTypes.Final"/>.
    /// </summary>
    public MessageActivityBuilder AddStreamFinal()
    {
        StreamInfoEntityExtensions.AddToActivity(_activity, StreamTypes.Final);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="MessageActivity"/> instance.
    /// </summary>
    public override MessageActivity Build() => _activity;

    private static TeamsAttachment BuildAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure)
    {
        ArgumentNullException.ThrowIfNull(adaptiveCard);
        TeamsAttachmentBuilder attachmentBuilder = TeamsAttachment.CreateBuilder().WithAdaptiveCard(adaptiveCard);
        configure?.Invoke(attachmentBuilder);
        return attachmentBuilder.Build();
    }
}
