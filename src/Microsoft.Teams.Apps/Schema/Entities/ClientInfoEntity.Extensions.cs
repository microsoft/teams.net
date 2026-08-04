// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Client info entity extension methods.
/// </summary>
public static class ClientInfoEntityExtensions
{
    /// <summary>
    /// Gets the first client information entity from the activity.
    /// </summary>
    public static ClientInfoEntity? GetClientInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is ClientInfoEntity) as ClientInfoEntity;
    }

    /// <summary>
    /// Internal helper to add client info to an activity.
    /// </summary>
    internal static ClientInfoEntity AddToActivity(TeamsActivityInput activity, string? platform, string? country, string? timezone, string? locale)
    {
        ArgumentNullException.ThrowIfNull(activity);

        activity.Entities ??= [];
        ClientInfoEntity entity = new()
        {
            Platform = platform,
            Country = country,
            Timezone = timezone,
            Locale = locale
        };

        activity.Entities.Add(entity);
        return entity;
    }
}
