// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Extension methods for Activity to handle quoted replies.
/// </summary>
[Experimental("ExperimentalTeamsQuotedReplies")]
public static class ActivityQuotedReplyExtensions
{
    /// <summary>
    /// Builds the inline placeholder element that pairs with a <see cref="QuotedReplyEntity"/>.
    /// XML-escapes <paramref name="messageId"/> so values containing &quot;, &lt;, &amp; etc. can't break out of the attribute.
    /// </summary>
    internal static string QuotedPlaceholder(string messageId)
        => $"<quoted messageId=\"{SecurityElement.Escape(messageId)}\"/>";

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
        return activity.Entities.OfType<QuotedReplyEntity>();
    }

    /// <summary>
    /// Add a quoted message reference and append a placeholder to the message text.
    /// Teams renders the quoted message as a preview bubble above the response text.
    /// If text is provided, it is appended to the quoted message placeholder.
    /// </summary>
    /// <param name="activity">The message activity to add the quote to. Cannot be null.</param>
    /// <param name="messageId">The ID of the message to quote. Cannot be null or whitespace.</param>
    /// <param name="text">Optional text, appended to the quoted message placeholder.</param>
    /// <returns>The created QuotedReplyEntity that was added to the activity.</returns>
    public static QuotedReplyEntity AddQuote(this MessageActivity activity, string messageId, string? text = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        QuotedReplyEntity entity = new() { QuotedReply = new QuotedReplyData { MessageId = messageId } };
        activity.Entities ??= [];
        activity.Entities.Add(entity);

        activity.Text = (activity.Text ?? "") + QuotedPlaceholder(messageId);
        if (text != null)
        {
            activity.Text += $" {text}";
        }

        return entity;
    }

    /// <summary>
    /// Prepend a QuotedReply entity and placeholder before existing text.
    /// Used by <see cref="Context{TActivity}.Reply(TeamsActivity, CancellationToken)"/> and
    /// <see cref="Context{TActivity}.Quote(string, TeamsActivity, CancellationToken)"/> for quote-above-response.
    /// </summary>
    /// <param name="activity">The message activity to prepend the quoted reply to.</param>
    /// <param name="messageId">The ID of the message to quote.</param>
    public static void PrependQuote(this MessageActivity activity, string messageId)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        activity.Entities ??= [];
        activity.Entities.Insert(0, new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });
        var placeholder = QuotedPlaceholder(messageId);
        var text = activity.Text?.Trim() ?? "";
        activity.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";
    }
}
