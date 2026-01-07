// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

namespace Microsoft.Teams.BotApps.Schema;

/// <summary>
/// Provides a fluent API for building TeamsActivity instances.
/// </summary>
public class TeamsActivityBuilder : CoreActivityBuilder<TeamsActivity, TeamsActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the TeamsActivityBuilder class.
    /// </summary>
    internal TeamsActivityBuilder() : base(TeamsActivity.FromActivity(new CoreActivity()))
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
    /// Sets the conversation (override for Teams-specific type).
    /// </summary>
    protected override void SetConversation(Conversation conversation)
    {
        _activity.Conversation = conversation is TeamsConversation teamsConv
            ? teamsConv
            : new TeamsConversation(conversation);
    }

    /// <summary>
    /// Sets the From account (override for Teams-specific type).
    /// </summary>
    protected override void SetFrom(ConversationAccount from)
    {
        _activity.From = from is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : new TeamsConversationAccount(from);
    }

    /// <summary>
    /// Sets the Recipient account (override for Teams-specific type).
    /// </summary>
    protected override void SetRecipient(ConversationAccount recipient)
    {
        _activity.Recipient = recipient is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : new TeamsConversationAccount(recipient);
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

        CoreActivity baseActivity = _activity;
        baseActivity.Entities = _activity.Entities.ToJsonArray();

        return this;
    }

    /// <summary>
    /// Builds and returns the configured TeamsActivity instance.
    /// </summary>
    /// <returns>The configured TeamsActivity.</returns>
    public override TeamsActivity Build()
    {
        _activity.Rebase();
        return _activity;
    }
}
