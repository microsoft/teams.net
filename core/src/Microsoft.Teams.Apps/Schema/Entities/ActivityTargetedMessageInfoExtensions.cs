// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Extension methods on <see cref="MessageActivity"/> for the Prompt Preview
/// targeted-message-info entity.
/// </summary>
[Experimental("ExperimentalTeamsTargeted")]
public static class ActivityTargetedMessageInfoExtensions
{
    /// <summary>
    /// Add a targeted message info entity for prompt preview.
    /// If an entity with type "targetedMessageInfo" already exists, it is not added again.
    /// Any existing "quotedReply" entities are removed from <see cref="TeamsActivity.Entities"/>
    /// and matching &lt;quoted messageId="..."/&gt; placeholders are stripped from
    /// <see cref="MessageActivity.Text"/> to prevent collision between quoted replies and
    /// prompt preview.
    /// </summary>
    /// <remarks>
    /// After the placeholder strip, <see cref="MessageActivity.Text"/> is trimmed of leading and
    /// trailing whitespace.
    /// </remarks>
    /// <param name="activity">The message activity to add the targeted message info to.</param>
    /// <param name="messageId">The ID of the targeted message.</param>
    /// <returns>The same activity, for chaining.</returns>
    public static MessageActivity AddTargetedMessageInfo(this MessageActivity activity, string messageId)
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

        if (activity.Text is not null)
        {
            string placeholder = $"<quoted messageId=\"{SecurityElement.Escape(messageId)}\"/>";
            activity.Text = activity.Text.Replace(placeholder, string.Empty, StringComparison.Ordinal).Trim();
        }

        bool hasEntity = activity.Entities?.Any(e => e.Type == "targetedMessageInfo") ?? false;
        if (!hasEntity)
        {
            activity.AddEntity(new TargetedMessageInfoEntity { MessageId = messageId });
        }

        return activity;
    }
}
