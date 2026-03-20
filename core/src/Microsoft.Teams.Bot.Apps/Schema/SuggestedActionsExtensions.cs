// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Extension methods for adding suggested actions to a <see cref="MessageActivity"/>.
/// </summary>
public static class SuggestedActionsExtensions
{
    /// <summary>
    /// Sets the suggested actions on the message activity.
    /// </summary>
    /// <param name="activity">The message activity. Cannot be null.</param>
    /// <param name="suggestedActions">The suggested actions to set.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity WithSuggestedActions(this MessageActivity activity, SuggestedActions suggestedActions)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.SuggestedActions = suggestedActions;
        return activity;
    }

    /// <summary>
    /// Adds suggested actions to the message activity. Creates the <see cref="SuggestedActions"/>
    /// instance if one does not already exist.
    /// </summary>
    /// <param name="activity">The message activity. Cannot be null.</param>
    /// <param name="actions">The card actions to add.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddSuggestedActions(this MessageActivity activity, params CardAction[] actions)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.SuggestedActions ??= new SuggestedActions();
        activity.SuggestedActions.AddActions(actions);
        return activity;
    }

    /// <summary>
    /// Adds suggested actions with specific recipients to the message activity. Creates the
    /// <see cref="SuggestedActions"/> instance if one does not already exist.
    /// </summary>
    /// <param name="activity">The message activity. Cannot be null.</param>
    /// <param name="recipients">The recipient IDs to target.</param>
    /// <param name="actions">The card actions to add.</param>
    /// <returns>The message activity for chaining.</returns>
    public static MessageActivity AddSuggestedActions(this MessageActivity activity, IEnumerable<string> recipients, params CardAction[] actions)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.SuggestedActions ??= new SuggestedActions();
        activity.SuggestedActions.AddRecipients(recipients.ToArray());
        activity.SuggestedActions.AddActions(actions);
        return activity;
    }
}
