// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Extension methods for Activity to handle quoted replies.
/// </summary>
[Experimental("ExperimentalTeamsQuotedReplies")]
public static class ActivityQuotedReplyExtensions
{
    /// <summary>
    /// Gets all quoted reply entities from the activity's entity collection.
    /// </summary>
    /// <param name="activity">The activity to extract quoted replies from. Cannot be null.</param>
    /// <returns>An enumerable of QuotedReplyEntity instances found in the activity's entities.</returns>
    public static IEnumerable<QuotedReplyEntity> GetQuotedMessages(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return [];
        }
        return activity.Entities.Where(e => e is QuotedReplyEntity).Cast<QuotedReplyEntity>();
    }

    /// <summary>
    /// Adds a quoted reply entity to the activity and appends a placeholder to the message text.
    /// </summary>
    /// <param name="activity">The activity to add the quoted reply to. Cannot be null.</param>
    /// <param name="messageId">The ID of the message to quote. Cannot be null or whitespace.</param>
    /// <param name="response">Optional response text to append after the placeholder.</param>
    /// <returns>The created QuotedReplyEntity that was added to the activity.</returns>
    public static QuotedReplyEntity AddQuotedReply(this TeamsActivity activity, string messageId, string? response = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        QuotedReplyEntity entity = new() { QuotedReply = new QuotedReplyData { MessageId = messageId } };
        activity.Entities ??= [];
        activity.Entities.Add(entity);

        if (activity is MessageActivity msg)
        {
            var placeholder = $"<quoted messageId=\"{messageId}\"/>";
            msg.Text = (msg.Text ?? "") + placeholder;
            if (response != null)
            {
                msg.Text += $" {response}";
            }
        }

        activity.Rebase();
        return entity;
    }

    /// <summary>
    /// Prepend a QuotedReply entity and placeholder before existing text.
    /// Used by ReplyAsync()/QuoteReplyAsync() for quote-above-response.
    /// </summary>
    /// <param name="activity">The message activity to prepend the quoted reply to.</param>
    /// <param name="messageId">The ID of the message to quote.</param>
    public static void PrependQuotedReply(this MessageActivity activity, string messageId)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        activity.Entities ??= [];
        activity.Entities.Add(new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });
        var placeholder = $"<quoted messageId=\"{messageId}\"/>";
        var text = activity.Text?.Trim() ?? "";
        activity.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";
        activity.Rebase();
    }
}
