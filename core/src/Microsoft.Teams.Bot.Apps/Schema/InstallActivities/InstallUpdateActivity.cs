// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.InstallActivities;

/// <summary>
/// Represents an installation update activity.
/// </summary>
public class InstallUpdateActivity : TeamsActivity
{
    /// <summary>
    /// Convenience method to create an InstallUpdateActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>An InstallUpdateActivity instance.</returns>
    public static new InstallUpdateActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new InstallUpdateActivity(activity);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public InstallUpdateActivity() : base(TeamsActivityType.InstallationUpdate)
    {
    }

    /// <summary>
    /// Internal constructor to create InstallUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected InstallUpdateActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("action", out var action))
        {
            Action = action?.ToString();
            activity.Properties.Remove("action");
        }
    }

    /// <summary>
    /// Gets or sets the action for the installation update. See <see cref="InstallUpdateActions"/> for known values.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }
}

/// <summary>
/// String constants for installation update actions.
/// </summary>
public static class InstallUpdateActions
{
    /// <summary>
    /// Add action constant.
    /// </summary>
    public const string Add = "add";

    /// <summary>
    /// Remove action constant.
    /// </summary>
    public const string Remove = "remove";
}
