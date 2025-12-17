// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents an installation update activity.
/// </summary>
public class InstallUpdateActivity : Activity
{
    /// <summary>
    /// Gets or sets the action for the installation update. See <see cref="InstallUpdateActions"/> for common values.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallUpdateActivity"/> class.
    /// </summary>
    public InstallUpdateActivity() : base(ActivityTypes.InstallationUpdate)
    {
    }
}

/// <summary>
/// String constants for installation update actions.
/// </summary>
public static class InstallUpdateActions
{
    /// <summary>
    /// Add action.
    /// </summary>
    public const string Add = "add";

    /// <summary>
    /// Remove action.
    /// </summary>
    public const string Remove = "remove";
}
