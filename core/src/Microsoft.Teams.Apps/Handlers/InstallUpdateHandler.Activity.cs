// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Utils;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

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
    internal InstallUpdateActivity() : base(TeamsActivityTypes.InstallationUpdate)
    {
    }

    /// <summary>
    /// Internal constructor to create InstallUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    internal InstallUpdateActivity(CoreActivity activity) : base(activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        Action = Properties.Extract<InstallUpdateAction>("action");
    }

    /// <summary>
    /// Gets or sets the action for the installation update. See <see cref="InstallUpdateActions"/> for known values.
    /// </summary>
    [JsonPropertyName("action")]
    public InstallUpdateAction? Action { get; internal set; }
}

/// <summary>
/// String constants for installation update actions.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<InstallUpdateAction>))]
public class InstallUpdateAction(string value) : StringEnum(value)
{
    /// <summary>Add action.</summary>
    public static readonly InstallUpdateAction Add = new("add");
    /// <summary>Remove action.</summary>
    public static readonly InstallUpdateAction Remove = new("remove");
}

/// <summary>
/// Common installation update action values.
/// </summary>
public static class InstallUpdateActions
{
    /// <summary>
    /// Add action constant.
    /// </summary>
    public static InstallUpdateAction Add => InstallUpdateAction.Add;

    /// <summary>
    /// Remove action constant.
    /// </summary>
    public static InstallUpdateAction Remove => InstallUpdateAction.Remove;
}
