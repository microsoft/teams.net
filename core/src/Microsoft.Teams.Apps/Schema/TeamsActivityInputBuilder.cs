// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Provides the shared fluent API for builders of outbound <see cref="TeamsActivityInput"/> subtypes
/// (for example <see cref="MessageActivityInputBuilder"/> and <see cref="StreamingActivityInputBuilder"/>).
/// </summary>
/// <typeparam name="TActivity">The concrete <see cref="TeamsActivityInput"/> type being built.</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent chaining).</typeparam>
public abstract class TeamsActivityInputBuilder<TActivity, TBuilder>
    where TActivity : TeamsActivityInput
    where TBuilder : TeamsActivityInputBuilder<TActivity, TBuilder>
{
    /// <summary>
    /// The activity being built.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly TActivity _activity;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// Initializes a new instance of the builder.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    protected TeamsActivityInputBuilder(TActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activity = activity;
    }

    // ==================== Activity metadata ====================

    /// <summary>
    /// Sets the activity ID.
    /// </summary>
    public TBuilder WithId(string id)
    {
        _activity.Id = id;
        return (TBuilder)this;
    }

    /// <summary>
    /// Builds and returns the configured activity instance.
    /// </summary>
    public abstract TActivity Build();

    // ==================== Entities ====================

    /// <summary>
    /// Sets the entities collection.
    /// </summary>
    public TBuilder WithEntities(EntityList entities)
    {
        _activity.Entities = entities;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds one or more entities to the activity.
    /// </summary>
    public TBuilder AddEntity(params Entity[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _activity.Entities ??= [];
        foreach (Entity entity in entities)
        {
            _activity.Entities.Add(entity);
        }
        return (TBuilder)this;
    }

    /// <summary>
    /// Replaces an existing entity with a new entity.
    /// </summary>
    public TBuilder UpdateEntity(Entity oldEntity, Entity newEntity)
    {
        ArgumentNullException.ThrowIfNull(oldEntity);
        ArgumentNullException.ThrowIfNull(newEntity);
        _activity.Entities ??= [];
        _activity.Entities.Remove(oldEntity);
        _activity.Entities.Add(newEntity);
        return (TBuilder)this;
    }

    // ==================== Suggested actions ====================

    /// <summary>
    /// Sets the suggested actions.
    /// </summary>
    public TBuilder WithSuggestedActions(SuggestedActions suggestedActions)
    {
        _activity.SuggestedActions = suggestedActions;
        return (TBuilder)this;
    }

    // ==================== Mentions / quotes / citations / feedback ====================

    /// <summary>
    /// Adds a mention (@mention) entity and optionally prepends mention text.
    /// </summary>
    public TBuilder AddMention(ChannelAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(account);
        MentionEntityExtensions.AddToActivity(_activity, account, text, addText);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a quoted message reference and appends a placeholder to the activity text.
    /// </summary>
    public TBuilder AddQuote(string messageId, string? text = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        QuotedReplyEntityExtensions.AddToActivity(_activity, messageId, text);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a targetedMessageInfo entity for Prompt Preview, referencing the inbound targeted-message id.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public TBuilder WithTargetedMessageInfo(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        TargetedMessageInfoEntityExtensions.AddToActivity(_activity, messageId);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a clientInfo entity to the activity.
    /// </summary>
    public TBuilder AddClientInfo(string? platform, string? country, string? timezone, string? locale)
    {
        ClientInfoEntityExtensions.AddToActivity(_activity, platform, country, timezone, locale);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a productInfo entity to the activity.
    /// </summary>
    public TBuilder AddProductInfo(string? id)
    {
        ProductInfoEntityExtensions.AddToActivity(_activity, id);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public TBuilder AddAIGenerated()
    {
        OMessageEntityExtensions.AddAIGeneratedContent(_activity);
        return (TBuilder)this;
    }

    /// <summary>
    /// Enables/disables the feedback loop on the activity.
    /// </summary>
    public TBuilder AddFeedback(bool value = true)
    {
        _activity.ChannelData ??= new TeamsOutboundChannelData();
        _activity.ChannelData.FeedbackLoopEnabled = value;
        return (TBuilder)this;
    }

    /// <summary>
    /// Configures the feedback loop mode on the activity.
    /// </summary>
    public TBuilder AddFeedback(string mode)
    {
        _activity.ChannelData ??= new TeamsOutboundChannelData();
        _activity.ChannelData.FeedbackLoop = new FeedbackLoop(mode);
        _activity.ChannelData.FeedbackLoopEnabled = null;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a citation claim to the activity.
    /// </summary>
    public TBuilder AddCitation(int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        _activity.Entities ??= [];
        CitationEntityExtensions.AddToActivity(_activity, position, appearance);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a content sensitivity label to the activity.
    /// </summary>
    public TBuilder AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null)
    {
        SensitiveUsageEntityExtensions.AddToActivity(_activity, name, description, pattern);
        return (TBuilder)this;
    }
}

/// <summary>
/// Provides a fluent API for building outbound <see cref="MessageActivityInput"/> instances.
/// </summary>
public class MessageActivityInputBuilder : TeamsActivityInputBuilder<MessageActivityInput, MessageActivityInputBuilder>
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
    public MessageActivityInputBuilder WithText(string text, string textFormat = TextFormats.Plain)
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
    public MessageActivityInputBuilder WithTextFormat(string textFormat)
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
    public MessageActivityInputBuilder WithAttachmentLayout(string attachmentLayout)
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

/// <summary>
/// Provides a fluent API for building outbound <see cref="StreamingActivityInput"/> instances.
/// </summary>
public class StreamingActivityInputBuilder : TeamsActivityInputBuilder<StreamingActivityInput, StreamingActivityInputBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivityInputBuilder"/> class.
    /// </summary>
    internal StreamingActivityInputBuilder() : base(new StreamingActivityInput())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingActivityInputBuilder"/> class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal StreamingActivityInputBuilder(StreamingActivityInput activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the accumulated text content of the streaming chunk.
    /// </summary>
    public StreamingActivityInputBuilder WithText(string text)
    {
        _activity.Text = text;
        return this;
    }

    /// <summary>
    /// Sets the stream metadata for this chunk (writes channel data and adds a <see cref="StreamInfoEntity"/>).
    /// </summary>
    /// <param name="streamType">The stream type. See <see cref="StreamTypes"/>.</param>
    /// <param name="streamId">Optional stream identifier.</param>
    /// <param name="streamSequence">Optional monotonically increasing sequence number.</param>
    public StreamingActivityInputBuilder WithStreamInfo(string streamType, string? streamId = null, int? streamSequence = null)
    {
        _activity.StreamInfo = StreamInfoEntityExtensions.AddToActivity(_activity, streamType, streamId, streamSequence);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="StreamingActivityInput"/> instance.
    /// </summary>
    public override StreamingActivityInput Build() => _activity;
}
