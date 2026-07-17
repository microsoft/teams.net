// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Product info entity extension methods.
/// </summary>
public static class ProductInfoEntityExtensions
{
    /// <summary>
    /// Gets the first product info entity from the activity.
    /// </summary>
    public static ProductInfoEntity? GetProductInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is ProductInfoEntity) as ProductInfoEntity;
    }

    /// <summary>
    /// Internal helper to add product info to an activity.
    /// </summary>
    internal static ProductInfoEntity AddToActivity(TeamsActivityInput activity, string? id)
    {
        ArgumentNullException.ThrowIfNull(activity);

        activity.Entities ??= [];
        ProductInfoEntity entity = new()
        {
            Id = id
        };

        activity.Entities.Add(entity);
        return entity;
    }
}
