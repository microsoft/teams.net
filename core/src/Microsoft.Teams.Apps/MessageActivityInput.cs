// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Represents an outbound message activity constructed by a builder and sent by the API clients.
/// </summary>
public class MessageActivityInput : TeamsActivityInput
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public MessageActivityInput() : base(TeamsActivityTypes.Message)
    {
    }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the text format. See <see cref="TextFormats"/> for common values.
    /// </summary>
    [JsonPropertyName("textFormat")]
    public TextFormat? TextFormat { get; set; }

    /// <summary>
    /// Gets or sets the attachments for the message.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IList<TeamsAttachment>? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the attachment layout.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public AttachmentLayoutType? AttachmentLayout { get; set; }

    /// <summary>
    /// Serializes the current activity to a JSON string using the outbound message serializer context.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public override string ToJson()
        => JsonSerializer.Serialize(this, TeamsActivityInputJsonContext.Default.MessageActivityInput);

    internal static new MessageActivityInputBuilder CreateBuilder() => new();

    private MessageActivityInput Apply(Action<MessageActivityInputBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(new MessageActivityInputBuilder(this));
        return this;
    }

    /// <summary>
    /// Sets the entities collection.
    /// </summary>
    public MessageActivityInput WithEntities(EntityList entities)
        => Apply(builder => builder.WithEntities(entities));

    /// <summary>
    /// Adds one or more entities to the activity.
    /// </summary>
    public MessageActivityInput AddEntity(params Entity[] entities)
        => Apply(builder => builder.AddEntity(entities));

    /// <summary>
    /// Replaces an existing entity with a new entity.
    /// </summary>
    public MessageActivityInput UpdateEntity(Entity oldEntity, Entity newEntity)
        => Apply(builder => builder.UpdateEntity(oldEntity, newEntity));

    /// <summary>
    /// Sets the text content (and optional default plain text format) of the message.
    /// </summary>
    public MessageActivityInput WithText(string text)
        => Apply(builder => builder.WithText(text));

    /// <summary>
    /// Sets the text content and format of the message.
    /// </summary>
    public MessageActivityInput WithText(string text, TextFormat textFormat)
        => Apply(builder => builder.WithText(text, textFormat));

    /// <summary>
    /// Appends text to the current message text.
    /// </summary>
    public MessageActivityInput AddText(string text)
        => Apply(builder => builder.AddText(text));

    /// <summary>
    /// Sets the text format. See <see cref="TextFormats"/>.
    /// </summary>
    public MessageActivityInput WithTextFormat(TextFormat textFormat)
        => Apply(builder => builder.WithTextFormat(textFormat));

    /// <summary>
    /// Sets the attachments collection.
    /// </summary>
    public MessageActivityInput WithAttachments(IList<TeamsAttachment> attachments)
        => Apply(builder => builder.WithAttachments(attachments));

    /// <summary>
    /// Adds one or more attachments to the message.
    /// </summary>
    public MessageActivityInput AddAttachment(params TeamsAttachment[] attachments)
        => Apply(builder => builder.AddAttachment(attachments));

    /// <summary>
    /// Sets the attachment layout (e.g., "list", "carousel").
    /// </summary>
    public MessageActivityInput WithAttachmentLayout(AttachmentLayoutType attachmentLayout)
        => Apply(builder => builder.WithAttachmentLayout(attachmentLayout));

    /// <summary>
    /// Adds an Adaptive Card attachment to the message.
    /// </summary>
    public MessageActivityInput AddAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
        => Apply(builder => builder.AddAdaptiveCardAttachment(adaptiveCard, configure));

    /// <summary>
    /// Sets the attachments collection to a single Adaptive Card attachment.
    /// </summary>
    public MessageActivityInput WithAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
        => Apply(builder => builder.WithAdaptiveCardAttachment(adaptiveCard, configure));

    /// <summary>
    /// Prepends a quoted message placeholder before existing text.
    /// </summary>
    public MessageActivityInput PrependQuote(string messageId)
        => Apply(builder => builder.PrependQuote(messageId));

    /// <summary>
    /// Marks the message as a final streaming message by adding a <see cref="StreamInfoEntity"/>
    /// with <see cref="StreamTypes.Final"/>.
    /// </summary>
    public MessageActivityInput AddStreamFinal()
        => Apply(builder => builder.AddStreamFinal());

    /// <summary>
    /// Sets the suggested actions.
    /// </summary>
    public MessageActivityInput WithSuggestedActions(SuggestedActions suggestedActions)
        => Apply(builder => builder.WithSuggestedActions(suggestedActions));

    /// <summary>
    /// Adds a mention (@mention) entity and optionally prepends mention text.
    /// </summary>
    public MessageActivityInput AddMention(TeamsChannelAccount account, string? text = null, bool addText = true)
        => Apply(builder => builder.AddMention(account, text, addText));

    /// <summary>
    /// Adds a quoted message reference and appends a placeholder to the activity text.
    /// </summary>
    public MessageActivityInput AddQuote(string messageId, string? text = null)
        => Apply(builder => builder.AddQuote(messageId, text));

    /// <summary>
    /// Adds a targetedMessageInfo entity for Prompt Preview, referencing the inbound targeted-message id.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public MessageActivityInput WithTargetedMessageInfo(string messageId)
        => Apply(builder => builder.WithTargetedMessageInfo(messageId));

    /// <summary>
    /// Adds a clientInfo entity to the activity.
    /// </summary>
    public MessageActivityInput AddClientInfo(string? platform, string? country, string? timezone, string? locale)
        => Apply(builder => builder.AddClientInfo(platform, country, timezone, locale));

    /// <summary>
    /// Adds a productInfo entity to the activity.
    /// </summary>
    public MessageActivityInput AddProductInfo(string? id)
        => Apply(builder => builder.AddProductInfo(id));

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public MessageActivityInput AddAIGenerated()
        => Apply(builder => builder.AddAIGenerated());

    /// <summary>
    /// Enables/disables the feedback loop on the activity.
    /// </summary>
    public MessageActivityInput AddFeedback(bool value = true)
        => Apply(builder => builder.AddFeedback(value));

    /// <summary>
    /// Configures the feedback loop mode on the activity.
    /// </summary>
    public MessageActivityInput AddFeedback(FeedbackType mode)
        => Apply(builder => builder.AddFeedback(mode));

    /// <summary>
    /// Adds a citation claim to the activity.
    /// </summary>
    public MessageActivityInput AddCitation(int position, CitationAppearance appearance)
        => Apply(builder => builder.AddCitation(position, appearance));

    /// <summary>
    /// Adds a content sensitivity label to the activity.
    /// </summary>
    public MessageActivityInput AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null)
        => Apply(builder => builder.AddSensitivityLabel(name, description, pattern));

    /// <summary>
    /// Sets the recipient account for the activity and marks whether the recipient is targeted
    /// (for example, a targeted message visible only to that recipient).
    /// </summary>
    /// <param name="account">The recipient account.</param>
    /// <param name="isTargeted">Whether the recipient is targeted.</param>
    /// <returns>The activity instance for chaining.</returns>
    [Experimental("ExperimentalTeamsTargeted")]
    public MessageActivityInput WithRecipient(TeamsChannelAccount account, bool isTargeted)
        => Apply(builder => builder.WithRecipient(account, isTargeted));
}

/// <summary>
/// Provides a fluent API for building outbound <see cref="MessageActivityInput"/> instances.
/// </summary>
internal class MessageActivityInputBuilder : TeamsActivityInputBuilder<MessageActivityInput, MessageActivityInputBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivityInputBuilder"/> class.
    /// </summary>
    internal MessageActivityInputBuilder() : base(new MessageActivityInput())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivityInputBuilder"/> class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal MessageActivityInputBuilder(MessageActivityInput activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the text content (and optional text format) of the message.
    /// </summary>
    public MessageActivityInputBuilder WithText(string text)
    {
        _activity.Text = text;
        _activity.TextFormat = TextFormats.Plain;
        return this;
    }

    /// <summary>
    /// Sets the text content and format of the message.
    /// </summary>
    public MessageActivityInputBuilder WithText(string text, TextFormat textFormat)
    {
        _activity.Text = text;
        _activity.TextFormat = textFormat;
        return this;
    }

    /// <summary>
    /// Appends text to the current message text.
    /// </summary>
    public MessageActivityInputBuilder AddText(string text)
    {
        _activity.Text = $"{_activity.Text}{text}";
        return this;
    }

    /// <summary>
    /// Sets the text format. See <see cref="TextFormats"/>.
    /// </summary>
    public MessageActivityInputBuilder WithTextFormat(TextFormat textFormat)
    {
        _activity.TextFormat = textFormat;
        return this;
    }

    /// <summary>
    /// Sets the attachments collection.
    /// </summary>
    public MessageActivityInputBuilder WithAttachments(IList<TeamsAttachment> attachments)
    {
        _activity.Attachments = attachments;
        return this;
    }

    /// <summary>
    /// Adds one or more attachments to the message.
    /// </summary>
    public MessageActivityInputBuilder AddAttachment(params TeamsAttachment[] attachments)
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
    public MessageActivityInputBuilder WithAttachmentLayout(AttachmentLayoutType attachmentLayout)
    {
        _activity.AttachmentLayout = attachmentLayout;
        return this;
    }

    /// <summary>
    /// Adds an Adaptive Card attachment to the message.
    /// </summary>
    public MessageActivityInputBuilder AddAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
        => AddAttachment(BuildAdaptiveCardAttachment(adaptiveCard, configure));

    /// <summary>
    /// Sets the attachments collection to a single Adaptive Card attachment.
    /// </summary>
    public MessageActivityInputBuilder WithAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
        => WithAttachments([BuildAdaptiveCardAttachment(adaptiveCard, configure)]);

    /// <summary>
    /// Prepends a quoted message placeholder before existing text.
    /// </summary>
    public MessageActivityInputBuilder PrependQuote(string messageId)
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
    public MessageActivityInputBuilder AddStreamFinal()
    {
        StreamInfoEntityExtensions.AddToActivity(_activity, StreamTypes.Final);
        return this;
    }

    // ==================== Suggested actions ====================

    /// <summary>
    /// Sets the suggested actions.
    /// </summary>
    public MessageActivityInputBuilder WithSuggestedActions(SuggestedActions suggestedActions)
    {
        _activity.SuggestedActions = suggestedActions;
        return this;
    }

    // ==================== Mentions / quotes / citations / feedback ====================

    /// <summary>
    /// Adds a mention (@mention) entity and optionally prepends mention text.
    /// </summary>
    public MessageActivityInputBuilder AddMention(TeamsChannelAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(account);
        MentionEntityExtensions.AddToActivity(_activity, account, text, addText);
        return this;
    }

    /// <summary>
    /// Adds a quoted message reference and appends a placeholder to the activity text.
    /// </summary>
    public MessageActivityInputBuilder AddQuote(string messageId, string? text = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        QuotedReplyEntityExtensions.AddToActivity(_activity, messageId, text);
        return this;
    }

    /// <summary>
    /// Adds a targetedMessageInfo entity for Prompt Preview, referencing the inbound targeted-message id.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public MessageActivityInputBuilder WithTargetedMessageInfo(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        TargetedMessageInfoEntityExtensions.AddToActivity(_activity, messageId);
        return this;
    }

    /// <summary>
    /// Adds a clientInfo entity to the activity.
    /// </summary>
    public MessageActivityInputBuilder AddClientInfo(string? platform, string? country, string? timezone, string? locale)
    {
        ClientInfoEntityExtensions.AddToActivity(_activity, platform, country, timezone, locale);
        return this;
    }

    /// <summary>
    /// Adds a productInfo entity to the activity.
    /// </summary>
    public MessageActivityInputBuilder AddProductInfo(string? id)
    {
        ProductInfoEntityExtensions.AddToActivity(_activity, id);
        return this;
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public MessageActivityInputBuilder AddAIGenerated()
    {
        OMessageEntityExtensions.AddAIGeneratedContent(_activity);
        return this;
    }

    /// <summary>
    /// Enables/disables the feedback loop on the activity.
    /// </summary>
    public MessageActivityInputBuilder AddFeedback(bool value = true)
    {
        _activity.ChannelData ??= new TeamsOutboundChannelData();
        _activity.ChannelData.FeedbackLoopEnabled = value;
        return this;
    }

    /// <summary>
    /// Configures the feedback loop mode on the activity.
    /// </summary>
    public MessageActivityInputBuilder AddFeedback(FeedbackType mode)
    {
        _activity.ChannelData ??= new TeamsOutboundChannelData();
        _activity.ChannelData.FeedbackLoop = new FeedbackLoop(mode);
        _activity.ChannelData.FeedbackLoopEnabled = null;
        return this;
    }

    /// <summary>
    /// Adds a citation claim to the activity.
    /// </summary>
    public MessageActivityInputBuilder AddCitation(int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        _activity.Entities ??= [];
        CitationEntityExtensions.AddToActivity(_activity, position, appearance);
        return this;
    }

    /// <summary>
    /// Adds a content sensitivity label to the activity.
    /// </summary>
    public MessageActivityInputBuilder AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null)
    {
        SensitiveUsageEntityExtensions.AddToActivity(_activity, name, description, pattern);
        return this;
    }

    /// <summary>
    /// Sets the recipient account for the activity and marks whether the recipient is targeted
    /// (for example, a targeted message visible only to that recipient).
    /// </summary>
    /// <param name="account">The recipient account.</param>
    /// <param name="isTargeted">Whether the recipient is targeted.</param>
    /// <returns>The builder instance for chaining.</returns>
    [Experimental("ExperimentalTeamsTargeted")]
    public MessageActivityInputBuilder WithRecipient(TeamsChannelAccount account, bool isTargeted)
    {
        ArgumentNullException.ThrowIfNull(account);
        account.IsTargeted = isTargeted ? true : null;
        _activity.Recipient = account;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="MessageActivityInput"/> instance.
    /// </summary>
    public override MessageActivityInput Build() => _activity;

    private static TeamsAttachment BuildAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure)
    {
        ArgumentNullException.ThrowIfNull(adaptiveCard);
        TeamsAttachmentBuilder attachmentBuilder = TeamsAttachment.CreateBuilder().WithAdaptiveCard(adaptiveCard);
        configure?.Invoke(attachmentBuilder);
        return attachmentBuilder.Build();
    }
}
