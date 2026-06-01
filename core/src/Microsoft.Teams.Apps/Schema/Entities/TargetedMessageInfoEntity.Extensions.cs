// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Targeted message info entity extension methods.
/// </summary>
[Experimental("ExperimentalTeamsTargeted")]
public static class TargetedMessageInfoEntityExtensions
{
    /// <summary>
    /// Builds the inline placeholder element that pairs with a <see cref="QuotedReplyEntity"/>.
    /// </summary>
    private static readonly Regex QuotedPlaceholderRegex = new("<quoted messageId=\"[^\"]*\"/>", RegexOptions.Compiled);

    /// <summary>
    /// Gets the first targeted message info entity from the activity.
    /// </summary>
    public static TargetedMessageInfoEntity? GetTargetedMessageInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is TargetedMessageInfoEntity) as TargetedMessageInfoEntity;
    }

    /// <summary>
    /// Adds targeted message info entity to a message and strips quote placeholders.
    /// Removes any existing quotedReply entities and their corresponding placeholder text.
    /// </summary>
    internal static void AddToActivity(TeamsActivity activity, string messageId)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        // Remove any existing quotedReply entities to prevent conflicts with the new targeted message info entity.
        if (activity.Entities is not null)
        {
            for (int i = activity.Entities.Count - 1; i >= 0; i--)
            {
                if (activity.Entities[i].Type == "quotedReply")
                {
                    activity.Entities.RemoveAt(i);
                }
            }
        }

        if (activity is MessageActivity message && message.Text is not null)
        {
            message.Text = QuotedPlaceholderRegex.Replace(message.Text, string.Empty).Trim();
        }
        else if (activity.Properties.TryGetValue("text", out object? rawText) && rawText is string text)
        {
            activity.Properties["text"] = QuotedPlaceholderRegex.Replace(text, string.Empty).Trim();
        }

        bool hasEntity = activity.Entities?.Any(e => e.Type == "targetedMessageInfo") ?? false;
        if (!hasEntity)
        {
            activity.Entities ??= [];
            activity.Entities.Add(new TargetedMessageInfoEntity { MessageId = messageId });
        }
    }
}
