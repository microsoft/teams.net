// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Provides a fluent API for building <see cref="MessageActivity"/> instances.
/// Uses typed property setters for first-class message fields (Text, TextFormat, SuggestedActions).
/// </summary>
public class MessageActivityBuilder : TeamsActivityBuilder<MessageActivity, MessageActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the MessageActivityBuilder class.
    /// </summary>
    internal MessageActivityBuilder() : base(new MessageActivity())
    {
    }

    /// <summary>
    /// Initializes a new instance of the MessageActivityBuilder class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal MessageActivityBuilder(MessageActivity activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the text content and text format of the message.
    /// </summary>
    public MessageActivityBuilder WithText(string text, string textFormat = "plain")
    {
        _activity.Text = text;
        _activity.TextFormat = textFormat;
        return this;
    }

    /// <summary>
    /// Sets the suggested actions for the message.
    /// </summary>
    public MessageActivityBuilder WithSuggestedActions(SuggestedActions suggestedActions)
    {
        _activity.SuggestedActions = suggestedActions;
        return this;
    }

    /// <summary>
    /// Adds a mention to the activity.
    /// </summary>
    /// <param name="account">The account to mention.</param>
    /// <param name="text">Optional custom text for the mention. If null, uses the account name.</param>
    /// <param name="addText">Whether to prepend the mention text to the activity's text content.</param>
    public MessageActivityBuilder AddMention(ConversationAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(account);
        string? mentionText = text ?? account.Name;

        if (addText)
        {
            _activity.Text = $"<at>{mentionText}</at> {_activity.Text}";
        }

        _activity.Entities ??= [];
        _activity.Entities.Add(new MentionEntity(account, $"<at>{mentionText}</at>"));

        return this;
    }

    /// <summary>
    /// Builds and returns the configured MessageActivity instance.
    /// </summary>
    public override MessageActivity Build()
    {
        _activity.Rebase();
        return _activity;
    }
}
