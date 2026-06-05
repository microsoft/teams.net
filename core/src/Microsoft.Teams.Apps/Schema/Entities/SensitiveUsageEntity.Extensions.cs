// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Sensitive usage entity extension methods.
/// </summary>
public static class SensitiveUsageEntityExtensions
{
    /// <summary>
    /// Gets all sensitivity label entities from the activity.
    /// </summary>
    public static IEnumerable<SensitiveUsageEntity> GetSensitivityLabels(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return [];
        }

        return activity.Entities.OfType<SensitiveUsageEntity>();
    }

    /// <summary>
    /// Internal helper to add a sensitivity label to an activity.
    /// </summary>
    internal static void AddToActivity(TeamsActivity activity, string name, string? description, DefinedTerm? pattern)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        activity.Entities ??= [];
        activity.Entities.Add(new SensitiveUsageEntity()
        {
            Name = name,
            Description = description,
            Pattern = pattern
        });
    }
}
