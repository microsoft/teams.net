// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Install update action constants.
/// </summary>
public static class InstallUpdateActions
{
    /// <summary>
    /// Add install update action.
    /// </summary>
    public const string Add = "add";

    /// <summary>
    /// Remove install update action.
    /// </summary>
    public const string Remove = "remove";
}

/// <summary>
/// Represents an installation update activity.
/// </summary>
public class InstallUpdateActivity : Activity
{
    /// <summary>
    /// Gets or sets the action (e.g., "add", "remove").
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallUpdateActivity"/> class.
    /// </summary>
    public InstallUpdateActivity() : base(ActivityTypes.InstallUpdate)
    {
    }
}
