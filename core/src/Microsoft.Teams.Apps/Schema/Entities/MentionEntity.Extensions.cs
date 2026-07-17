// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Mention entity extension methods.
/// </summary>
public static class MentionEntityExtensions
{
    /// <summary>
    /// Gets all mention entities from the activity.
    /// </summary>
    public static IEnumerable<MentionEntity> GetMentions(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return [];
        }

        return activity.Entities.Where(e => e is MentionEntity).Cast<MentionEntity>();
    }

    /// <summary>
    /// Internal helper to add a mention to an activity.
    /// </summary>
    internal static void AddToActivity(TeamsActivityInput activity, ChannelAccount account, string? text, bool addText)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(account);

        string? mentionText = text ?? account.Name;

        if (addText)
        {
            string? currentText = activity is MessageActivityInput message
                ? message.Text
                : (activity.Properties.TryGetValue("text", out object? value) ? value?.ToString() : null);

            string updatedText = $"<at>{mentionText}</at> {currentText}";

            if (activity is MessageActivityInput msg)
            {
                msg.Text = updatedText;
            }
            else
            {
                activity.Properties["text"] = updatedText;
            }
        }

        activity.Entities ??= [];
        activity.Entities.Add(new MentionEntity(account, $"<at>{mentionText}</at>"));
    }
}
