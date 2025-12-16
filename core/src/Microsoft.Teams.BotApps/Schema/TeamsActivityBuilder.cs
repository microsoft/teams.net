// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

namespace Microsoft.Teams.BotApps.Schema;

/// <summary>
/// Provides a fluent API for building TeamsActivity instances.
/// </summary>
public class TeamsActivityBuilder
{
    private readonly TeamsActivity _activity;

    /// <summary>
    /// Initializes a new instance of the TeamsActivityBuilder class.
    /// </summary>
    public TeamsActivityBuilder()
    {
        _activity = TeamsActivity.FromActivity(new CoreActivity());
    }

    /// <summary>
    /// Initializes a new instance of the TeamsActivityBuilder class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    public TeamsActivityBuilder(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activity = activity;
    }

    /// <summary>
    /// Apply Conversation Reference
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public TeamsActivityBuilder WithConversationReference(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ChannelId);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentNullException.ThrowIfNull(activity.From);
        ArgumentNullException.ThrowIfNull(activity.Recipient);

        this
            .WithServiceUrl(activity.ServiceUrl)
            .WithChannelId(activity.ChannelId)
            .WithConversation(activity.Conversation)
            .WithFrom(activity.Recipient)
            .WithRecipient(activity.From);

        return this;

    }


    /// <summary>
    /// Sets the activity ID.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithId(string id)
    {
        _activity.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the service URL.
    /// </summary>
    /// <param name="serviceUrl">The service URL.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithServiceUrl(Uri serviceUrl)
    {
        _activity.ServiceUrl = serviceUrl;
        return this;
    }

    /// <summary>
    /// Sets the channel ID.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithChannelId(string channelId)
    {
        _activity.ChannelId = channelId;
        return this;
    }

    /// <summary>
    /// Sets the activity type.
    /// </summary>
    /// <param name="type">The activity type.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithType(string type)
    {
        _activity.Type = type;
        return this;
    }

    /// <summary>
    /// Sets the text content.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithText(string text)
    {
        _activity.Text = text;
        return this;
    }

    /// <summary>
    /// Sets the sender account information.
    /// </summary>
    /// <param name="from">The sender account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithFrom(TeamsConversationAccount from)
    {
        _activity.From = from;
        return this;
    }

    /// <summary>
    /// Sets the recipient account information.
    /// </summary>
    /// <param name="recipient">The recipient account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithRecipient(TeamsConversationAccount recipient)
    {
        _activity.Recipient = recipient;
        return this;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    /// <param name="conversation">The conversation information.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithConversation(TeamsConversation conversation)
    {
        _activity.Conversation = conversation;
        return this;
    }

    /// <summary>
    /// Sets the Teams-specific channel data.
    /// </summary>
    /// <param name="channelData">The channel data.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TeamsActivityBuilder WithChannelData(TeamsChannelData channelData)
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
            _activity.Text = $"<at>{mentionText}</at> {_activity.Text}";
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
    public TeamsActivity Build()
    {
        _activity.Rebase();
        return _activity;
    }
}
