// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Quoted reply entity extension methods.
/// </summary>
[Experimental("ExperimentalTeamsQuotedReplies")]
public static class QuotedReplyEntityExtensions
{
    /// <summary>
    /// Builds the inline placeholder string for a quoted reply.
    /// </summary>
    internal static string QuotedPlaceholder(string messageId)
        => $"<quoted messageId=\"{SecurityElement.Escape(messageId)}\"/>";

    /// <summary>
    /// Gets all quoted reply entities from the activity.
    /// </summary>
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
    /// Internal helper to add a quoted-reply entity and append the quoted placeholder to activity text.
    /// </summary>
    internal static void AddToActivity(TeamsActivity activity, string messageId, string? text = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        QuotedReplyEntity entity = new() { QuotedReply = new QuotedReplyData { MessageId = messageId } };
        activity.Entities ??= [];
        activity.Entities.Add(entity);

        string currentText = activity is MessageActivity message
            ? (message.Text ?? string.Empty)
            : (activity.Properties.TryGetValue("text", out object? value) ? value?.ToString() ?? string.Empty : string.Empty);

        string newText = currentText + QuotedPlaceholder(messageId);
        if (text != null)
        {
            newText += $" {text}";
        }

        if (activity is MessageActivity msg)
        {
            msg.Text = newText;
        }
        else
        {
            activity.Properties["text"] = newText;
        }
    }
}
