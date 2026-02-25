// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Extension methods for <see cref="TeamsActivity"/>.
/// </summary>
public static class TeamsActivityExtensions
{
    /// <summary>
    /// Gets the name of the activity, if applicable.
    /// Returns the <c>Name</c> for <see cref="InvokeActivity"/> and <see cref="EventActivity"/>; otherwise <c>null</c>.
    /// </summary>
    public static string? GetName(this TeamsActivity activity)
        => (activity as InvokeActivity)?.Name ?? (activity as EventActivity)?.Name;
}
