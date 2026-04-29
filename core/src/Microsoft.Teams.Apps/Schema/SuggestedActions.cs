// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents suggested actions that can be shown to the user as quick reply buttons.
/// </summary>
public class SuggestedActions
{
    /// <summary>
    /// Gets or sets the IDs of the recipients that the actions should be shown to.
    /// These IDs are relative to the channelId and a subset of all recipients of the activity.
    /// </summary>
    [JsonPropertyName("to")]
    public IList<string> To { get; set; } = [];

    /// <summary>
    /// Gets or sets the actions that can be shown to the user.
    /// </summary>
    [JsonPropertyName("actions")]
    public IList<SuggestedAction> Actions { get; set; } = [];

    /// <summary>
    /// Adds recipients to the suggested actions.
    /// </summary>
    /// <param name="recipients">The recipient IDs to add.</param>
    /// <returns>This instance for chaining.</returns>
    public SuggestedActions AddRecipients(params string[] recipients)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        foreach (string to in recipients)
        {
            To.Add(to);
        }

        return this;
    }

    /// <summary>
    /// Adds a single action to the suggested actions.
    /// </summary>
    /// <param name="action">The action to add.</param>
    /// <returns>This instance for chaining.</returns>
    public SuggestedActions AddAction(SuggestedAction action)
    {
        Actions.Add(action);
        return this;
    }

    /// <summary>
    /// Adds multiple actions to the suggested actions.
    /// </summary>
    /// <param name="actions">The actions to add.</param>
    /// <returns>This instance for chaining.</returns>
    public SuggestedActions AddActions(params SuggestedAction[] actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        foreach (SuggestedAction action in actions)
        {
            Actions.Add(action);
        }

        return this;
    }
}
