// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

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
        _activity.Conversation = activity.Conversation;
        _activity.From = activity.Recipient;

        if (!string.IsNullOrEmpty(activity.Id))
        {
            WithReplyToId(activity.Id);
        }

        return this;
    }

    /// <summary>
    /// Sets the sender account information.
    /// </summary>
    /// <param name="from">The sender account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithFrom(ConversationAccount? from)
    {
        _activity.From = from is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(from);
        return this;
    }

    /// <summary>
    /// Sets the recipient account information.
    /// </summary>
    /// <param name="recipient">The recipient account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithRecipient(ConversationAccount? recipient)
    {
        _activity.Recipient = recipient is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(recipient);
        return this;
    }

    /// <summary>
    /// Sets the recipient account information and optionally marks this as a targeted message.
    /// </summary>
    /// <param name="recipient">The recipient account.</param>
    /// <param name="isTargeted">If true, marks this as a targeted message visible only to the specified recipient.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithRecipient(ConversationAccount? recipient, bool isTargeted)
    {
        if (recipient is null)
        {
            _activity.Recipient = null;
        }
        else
        {
            recipient.IsTargeted = isTargeted ? true : null;
            _activity.Recipient = recipient is TeamsConversationAccount teamsAccount
                ? teamsAccount
                : TeamsConversationAccount.FromConversationAccount(recipient);
        }
        return this;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    /// <param name="conversation">The conversation information.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithConversation(Conversation? conversation)
    {
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
        _activity.Attachments = attachments;
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
        _activity.Attachments = attachment is null
            ? null
            : [attachment];

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
        _activity.Attachments ??= [];
        _activity.Attachments.Add(attachment);
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
        WithProperty("text", text);
        WithProperty("textFormat", textFormat);
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
    /// Adds a mention to the activity.
    /// </summary>
    /// <param name="account">The account to mention.</param>
    /// <param name="text">Optional custom text for the mention. If null, uses the account name.</param>
    /// <param name="addText">Whether to prepend the mention text to the activity's text content.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder AddMention(ConversationAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(account);
        string? mentionText = text ?? account.Name;

        if (addText)
        {
            string? currentText = _activity.Properties.TryGetValue("text", out object? value) ? value?.ToString() : null;
            WithProperty("text", $"<at>{mentionText}</at> {currentText}");
        }

        _activity.Entities ??= [];
        _activity.Entities.Add(new MentionEntity(account, $"<at>{mentionText}</at>"));

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
