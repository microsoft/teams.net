// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Extension methods on <see cref="TeamsActivity"/> for the Prompt Preview
/// targeted-message-info entity.
/// </summary>
[Experimental("ExperimentalTeamsTargeted")]
public static partial class ActivityTargetedMessageInfoExtensions
{
    [GeneratedRegex("<quoted messageId=\"[^\"]*\"/>")]
    internal static partial Regex QuotedPlaceholderRegex();

    /// <summary>
    /// Add a targeted message info entity for prompt preview.
    /// If an entity with type "targetedMessageInfo" already exists, it is not added again.
    /// Any existing "quotedReply" entities are removed from <see cref="TeamsActivity.Entities"/>
    /// and any &lt;quoted messageId="..."/&gt; placeholders are stripped from the activity text
    /// to prevent collision between quoted replies and prompt preview.
    /// </summary>
    /// <remarks>
    /// After the placeholder strip, the activity text is trimmed of leading and trailing whitespace.
    /// </remarks>
    /// <typeparam name="T">The concrete activity type, preserved for fluent chaining.</typeparam>
    /// <param name="activity">The activity to add the targeted message info to.</param>
    /// <param name="messageId">The ID of the targeted message.</param>
    /// <returns>The same activity, for chaining.</returns>
    public static T AddTargetedMessageInfo<T>(this T activity, string messageId) where T : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

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

        if (activity is MessageActivity msg && msg.Text is not null)
        {
            msg.Text = QuotedPlaceholderRegex().Replace(msg.Text, string.Empty).Trim();
        }
        else if (activity.Properties.TryGetValue("text", out object? rawText) && rawText is string text)
        {
            activity.Properties["text"] = QuotedPlaceholderRegex().Replace(text, string.Empty).Trim();
        }

        bool hasEntity = activity.Entities?.Any(e => e.Type == "targetedMessageInfo") ?? false;
        if (!hasEntity)
        {
            activity.AddEntity(new TargetedMessageInfoEntity { MessageId = messageId });
        }

        return activity;
    }
}
