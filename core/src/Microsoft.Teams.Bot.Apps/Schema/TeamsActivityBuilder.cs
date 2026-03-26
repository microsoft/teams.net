// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Abstract generic base for Teams activity builders.
/// Provides Teams-specific overrides and fluent methods common to all Teams activity types.
/// </summary>
/// <typeparam name="TActivity">The concrete Teams activity type being built.</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent chaining).</typeparam>
public abstract class TeamsActivityBuilder<TActivity, TBuilder> : CoreActivityBuilder<TActivity, TBuilder>
    where TActivity : TeamsActivity
    where TBuilder : TeamsActivityBuilder<TActivity, TBuilder>
{
    /// <summary>
    /// Initializes a new instance with the given activity.
    /// </summary>
    protected TeamsActivityBuilder(TActivity activity) : base(activity)
    {
    }

    /// <inheritdoc/>
    protected override void SetConversation(Conversation? conversation)
    {
        _activity.Conversation = conversation is TeamsConversation teamsConv
            ? teamsConv
            : TeamsConversation.FromConversation(conversation);
    }

    /// <inheritdoc/>
    protected override void SetFrom(ConversationAccount? from)
    {
        _activity.From = from is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(from);
    }

    /// <inheritdoc/>
    protected override void SetRecipient(ConversationAccount? recipient)
    {
        _activity.Recipient = recipient is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(recipient);
    }

    /// <summary>
    /// Sets the Teams-specific channel data.
    /// </summary>
    public TBuilder WithChannelData(TeamsChannelData? channelData)
    {
        _activity.ChannelData = channelData;
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the entities collection.
    /// </summary>
    public TBuilder WithEntities(EntityList entities)
    {
        _activity.Entities = entities;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds an entity to the activity's Entities collection.
    /// </summary>
    public TBuilder AddEntity(Entity entity)
    {
        _activity.Entities ??= [];
        _activity.Entities.Add(entity);
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the attachments collection.
    /// </summary>
    public TBuilder WithAttachments(IList<TeamsAttachment> attachments)
    {
        _activity.Attachments = attachments;
        return (TBuilder)this;
    }


    /// <summary>
    /// Adds an attachment to the activity's Attachments collection.
    /// </summary>
    public TBuilder AddAttachment(TeamsAttachment attachment)
    {
        _activity.Attachments ??= [];
        _activity.Attachments.Add(attachment);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds an Adaptive Card attachment to the activity.
    /// </summary>
    /// <param name="adaptiveCard">The Adaptive Card payload.</param>
    /// <param name="configure">Optional callback to further configure the attachment.</param>
    public TBuilder AddAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(adaptiveCard);
        return AddAttachment(BuildAdaptiveCardAttachment(adaptiveCard, configure));
    }

    private static TeamsAttachment BuildAdaptiveCardAttachment(object adaptiveCard, Action<TeamsAttachmentBuilder>? configure)
    {
        TeamsAttachmentBuilder attachmentBuilder = TeamsAttachment
            .CreateBuilder()
            .WithAdaptiveCard(adaptiveCard);

        configure?.Invoke(attachmentBuilder);

        return attachmentBuilder.Build();
    }
}

/// <summary>
/// Provides a fluent API for building <see cref="TeamsActivity"/> instances.
/// </summary>
public class TeamsActivityBuilder : TeamsActivityBuilder<TeamsActivity, TeamsActivityBuilder>
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
    /// Builds and returns the configured TeamsActivity instance.
    /// </summary>
    public override TeamsActivity Build()
    {
        _activity.Rebase();
        return _activity;
    }
}
