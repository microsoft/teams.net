// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Provides a fluent API for building TeamsActivity instances.
/// </summary>
public class TeamsActivityBuilder : CoreActivityBuilder<TeamsActivity, TeamsActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the TeamsActivityBuilder class.
    /// </summary>
    internal TeamsActivityBuilder() : base(new TeamsActivity())
    {
    }

    /// <summary>
    /// Initializes a new instance of the TeamsActivityBuilder class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal TeamsActivityBuilder(TeamsActivity activity) : base(activity)
    {
    }

    /// <summary>
    /// Apply Conversation Reference from the specified activity.
    /// </summary>
    /// <param name="activity">The source activity to copy conversation reference from.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithConversationReference(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ChannelId);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentNullException.ThrowIfNull(activity.From);
        ArgumentNullException.ThrowIfNull(activity.Recipient);

        WithServiceUrl(activity.ServiceUrl);
        WithChannelId(activity.ChannelId);
        WithConversation(activity.Conversation);
        WithFrom(activity.Recipient);

        return this;
    }

    /// <summary>
    /// Sets the sender account information.
    /// </summary>
    /// <param name="from">The sender account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public new TeamsActivityBuilder WithFrom(ConversationAccount? from)
    {
        _activity.From = from is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(from)!;
        return this;
    }

    /// <summary>
    /// Sets the recipient account information.
    /// </summary>
    /// <param name="recipient">The recipient account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public new TeamsActivityBuilder WithRecipient(ConversationAccount? recipient)
    {
        _activity.Recipient = recipient is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(recipient)!;
        return this;
    }

    /// <summary>
    /// Sets the recipient account information and optionally marks this as a targeted message.
    /// </summary>
    /// <param name="recipient">The recipient account.</param>
    /// <param name="isTargeted">If true, marks this as a targeted message visible only to the specified recipient.</param>
    /// <returns>The builder instance for chaining.</returns>
    [Experimental("ExperimentalTeamsTargeted")]
    public TeamsActivityBuilder WithRecipient(ConversationAccount? recipient, bool isTargeted)
    {
        if (recipient is not null)
        {
            recipient.IsTargeted = isTargeted ? true : null;
            _activity.Recipient = recipient is TeamsConversationAccount teamsAccount
                ? teamsAccount
                : TeamsConversationAccount.FromConversationAccount(recipient)!;
        }
        return this;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    /// <param name="conversation">The conversation information.</param>
    /// <returns>The builder instance for chaining.</returns>
    public new TeamsActivityBuilder WithConversation(Conversation? conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        _activity.Conversation = conversation is TeamsConversation teamsConv
            ? teamsConv
            : TeamsConversation.FromConversation(conversation);

        return this;
    }

    /// <summary>
    /// Sets the Teams-specific channel data.
    /// </summary>
    /// <param name="channelData">The channel data.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithChannelData(TeamsChannelData? channelData)
    {
        _activity.ChannelData = channelData;
        return this;
    }

    /// <summary>
    /// Sets the entities collection.
    /// </summary>
    /// <param name="entities">The entities collection.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithEntities(EntityList entities)
    {
        _activity.Entities = entities;
        return this;
    }

    /// <summary>
    /// Sets the attachments collection.
    /// </summary>
    /// <param name="attachments">The attachments collection.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithAttachments(IList<TeamsAttachment> attachments)
    {
        if (_activity is MessageActivity msg)
            msg.Attachments = attachments;
        else
            _activity.Properties["attachments"] = attachments;
        return this;
    }

    // TODO: Builders should only have "With" methods, not "Add" methods.
    /// <summary>
    /// Replaces the attachments collection with a single attachment.
    /// </summary>
    /// <param name="attachment">The attachment to set. Passing null clears the attachments.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithAttachment(TeamsAttachment? attachment)
    {
        IList<TeamsAttachment>? attachments = attachment is null ? null : [attachment];
        if (_activity is MessageActivity msg)
            msg.Attachments = attachments;
        else
            _activity.Properties["attachments"] = attachments;
        return this;
    }

    /// <summary>
    /// Adds an entity to the activity's Entities collection.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddEntity(Entity entity)
    {
        _activity.Entities ??= [];
        _activity.Entities.Add(entity);
        return this;
    }

    /// <summary>
    /// Adds an attachment to the activity's Attachments collection.
    /// </summary>
    /// <param name="attachment">The attachment to add.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddAttachment(TeamsAttachment attachment)
    {
        if (_activity is MessageActivity msg)
        {
            msg.Attachments ??= [];
            msg.Attachments.Add(attachment);
        }
        else
        {
            if (!_activity.Properties.TryGetValue("attachments", out object? existing) || existing is not List<TeamsAttachment> list)
            {
                list = [];
                _activity.Properties["attachments"] = list;
            }
            list.Add(attachment);
        }
        return this;
    }

    /// <summary>
    /// Adds an Adaptive Card attachment to the activity.
    /// </summary>
    /// <param name="adaptiveCard">The Adaptive Card payload.</param>
    /// <param name="configure">Optional callback to further configure the attachment before it is added.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
    {
        TeamsAttachment attachment = BuildAdaptiveCardAttachment(adaptiveCard, configure);
        return AddAttachment(attachment);
    }

    /// <summary>
    /// Sets the activity attachments collection to a single Adaptive Card attachment.
    /// </summary>
    /// <param name="adaptiveCard">The Adaptive Card payload.</param>
    /// <param name="configure">Optional callback to further configure the attachment.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
    {
        TeamsAttachment attachment = BuildAdaptiveCardAttachment(adaptiveCard, configure);
        return WithAttachment(attachment);
    }

    /// <summary>
    /// Adds or sets the text content of the activity.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="textFormat"></param>
    /// <returns></returns>
    public TeamsActivityBuilder WithText(string text, string textFormat = "plain")
    {
        if (_activity is MessageActivity msg)
        {
            msg.Text = text;
            msg.TextFormat = textFormat;
        }
        else
        {
            _activity.Properties["text"] = text;
            _activity.Properties["textFormat"] = textFormat;
        }
        return this;
    }

    /// <summary>
    /// With Suggested Actions
    /// </summary>
    /// <param name="suggestedActions"></param>
    /// <returns></returns>
    public TeamsActivityBuilder WithSuggestedActions(SuggestedActions suggestedActions)
    {
        ArgumentNullException.ThrowIfNull(_activity);
        _activity.SuggestedActions = suggestedActions;
        return this;
    }

    /// <summary>
    /// Adds a quoted reply entity and appends a placeholder to the activity text.
    /// The activity type must be set to Message (via <see cref="CoreActivityBuilder{TActivity,TBuilder}.WithType"/>) before calling this method.
    /// </summary>
    /// <param name="messageId">The ID of the message to quote.</param>
    /// <param name="text">Optional text, appended to the quoted message placeholder.</param>
    /// <returns>The builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity type is not Message.</exception>
    public TeamsActivityBuilder AddQuote(string messageId, string? text = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (_activity.Type != TeamsActivityType.Message)
        {
            throw new InvalidOperationException("AddQuote can only be used on message activities. Call WithType(TeamsActivityType.Message) first.");
        }

        QuotedReplyEntityExtensions.AddToActivity(_activity, messageId, text);

        return this;
    }

    /// <summary>
    /// Adds a targetedMessageInfo entity for Prompt Preview, referencing the inbound targeted-message id.
    /// Any existing quotedReply entities and matching &lt;quoted messageId="..."/&gt; placeholders are stripped
    /// to prevent collision with prompt preview. If a targetedMessageInfo entity is already present, no new entity is added.
    /// The activity type must be set to Message (via <see cref="CoreActivityBuilder{TActivity,TBuilder}.WithType"/>) before calling this method.
    /// </summary>
    /// <remarks>
    /// After the placeholder strip, the activity text is trimmed of leading and trailing whitespace.
    /// </remarks>
    /// <param name="messageId">The id of the inbound targeted message being responded to.</param>
    /// <returns>The builder instance for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity type is not Message.</exception>
    [Experimental("ExperimentalTeamsTargeted")]
    public TeamsActivityBuilder WithTargetedMessageInfo(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (_activity.Type != TeamsActivityType.Message)
        {
            throw new InvalidOperationException("WithTargetedMessageInfo can only be used on message activities. Call WithType(TeamsActivityType.Message) first.");
        }

        TargetedMessageInfoEntityExtensions.AddToActivity(_activity, messageId);

        return this;
    }

    /// <summary>
    /// Adds a mention to the activity.
    /// </summary>
    /// <param name="account">The account to mention.</param>
    /// <param name="text">Optional custom text for the mention. If null, uses the account name.</param>
    /// <param name="addText">Whether to prepend the mention text to the activity's text content.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddMention(ConversationAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(account);
        MentionEntityExtensions.AddToActivity(_activity, account, text, addText);
        return this;
    }

    /// <summary>
    /// Adds a clientInfo entity to the activity.
    /// </summary>
    /// <param name="platform">The client platform (for example, Web or Desktop).</param>
    /// <param name="country">The client's country/region code.</param>
    /// <param name="timezone">The client's IANA timezone.</param>
    /// <param name="locale">The client's locale (for example, en-US).</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddClientInfo(string? platform, string? country, string? timezone, string? locale)
    {
        ClientInfoEntityExtensions.AddToActivity(_activity, platform, country, timezone, locale);
        return this;
    }

    /// <summary>
    /// Adds a productInfo entity to the activity.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddProductInfo(string? id)
    {
        ProductInfoEntityExtensions.AddToActivity(_activity, id);
        return this;
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public TeamsActivityBuilder AddAIGenerated()
    {
        ArgumentNullException.ThrowIfNull(_activity);

        OMessageEntityExtensions.AddAIGeneratedContent(_activity);
        return this;
    }

    /// <summary>
    /// Enables/disables feedback loop on the activity.
    /// </summary>
    public TeamsActivityBuilder AddFeedback(bool value = true)
    {
        ArgumentNullException.ThrowIfNull(_activity);

        _activity.ChannelData ??= new TeamsChannelData();
        _activity.ChannelData.FeedbackLoopEnabled = value;
        return this;
    }

    /// <summary>
    /// Configures feedback loop mode on the activity.
    /// </summary>
    /// <param name="mode">The feedback loop type. See <see cref="FeedbackType"/> for known values.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddFeedback(string mode)
    {
        ArgumentNullException.ThrowIfNull(_activity);

        _activity.ChannelData ??= new TeamsChannelData();
        _activity.ChannelData.FeedbackLoop = new FeedbackLoop(mode);
        _activity.ChannelData.FeedbackLoopEnabled = null;
        return this;
    }

    /// <summary>
    /// Adds a citation claim to the activity.
    /// </summary>
    public TeamsActivityBuilder AddCitation(int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(_activity);
        ArgumentNullException.ThrowIfNull(appearance);

        _activity.Entities ??= [];
        CitationEntityExtensions.AddToActivity(_activity, position, appearance);
        return this;
    }

    /// <summary>
    /// Adds a content sensitivity label to the activity.
    /// </summary>
    /// <param name="name">The name of the sensitivity label.</param>
    /// <param name="description">Optional description of the sensitivity label.</param>
    /// <param name="pattern">Optional pattern associated with the sensitivity label.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(_activity);
        SensitiveUsageEntityExtensions.AddToActivity(_activity, name, description, pattern);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured TeamsActivity instance.
    /// </summary>
    /// <returns>The configured TeamsActivity.</returns>
    public override TeamsActivity Build()
    {
        return _activity;
    }

    private static TeamsAttachment BuildAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure)
    {
        ArgumentNullException.ThrowIfNull(adaptiveCard);

        TeamsAttachmentBuilder attachmentBuilder = TeamsAttachment
            .CreateBuilder()
            .WithAdaptiveCard(adaptiveCard);

        configure?.Invoke(attachmentBuilder);

        return attachmentBuilder.Build();
    }
}
